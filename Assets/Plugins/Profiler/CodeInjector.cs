﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Based on: http://www.codersblock.org/blog//2014/06/integrating-monocecil-with-unity.html

/* Todo:
* Start patching firstpass and unityengine libraries, making sure to not touch Mono.Cecil and Profiler code.
*/

[InitializeOnLoad]
public static class AssemblyPostProcessor {
    static AssemblyPostProcessor() {
        Debug.Log("AssemblyPostProcessor running");

        // Lock assemblies while they may be altered
        EditorApplication.LockReloadAssemblies();

        // This will hold the paths to all the assemblies that will be processed
        HashSet<string> assemblyPaths = new HashSet<string>();
        // This will hold the search directories for the resolver
        HashSet<string> assemblySearchDirectories = new HashSet<string>();

        // Add all assemblies in the project to be processed, and add their directory to
        // the resolver search directories.
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
            // Only process assemblies which are in the project
            if (assembly.Location.Replace('\\', '/').StartsWith(Application.dataPath.Substring(0, Application.dataPath.Length - 7))) {
                Debug.Log("Adding assembly for patching: " + assembly.Location);
                assemblyPaths.Add(assembly.Location); 
            }
            // But always add the assembly folder to the search directories
            assemblySearchDirectories.Add(Path.GetDirectoryName(assembly.Location));
        }

        string unityEngineLibPath = Application.dataPath.Substring(0, Application.dataPath.Length - 7) +
                                    "/Library/UnityAssemblies/UnityEngine.dll";
        string unityEngineLibBackupPath = Application.dataPath.Substring(0, Application.dataPath.Length - 7) +
                                    "/Library/UnityAssemblies/UnityEngine_.dll";

        File.Copy(unityEngineLibBackupPath, unityEngineLibPath, true);

        assemblyPaths.Add(unityEngineLibPath);

        // Create resolver
        DefaultAssemblyResolver assemblyResolver = new DefaultAssemblyResolver();
        // Add all directories found in the project folder
        foreach (String searchDirectory in assemblySearchDirectories) {
            assemblyResolver.AddSearchDirectory(searchDirectory);
        }
        // Add path to the Unity managed dlls
        assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data/Managed");

        // Create reader parameters with resolver
        ReaderParameters readerParameters = new ReaderParameters();
        readerParameters.AssemblyResolver = assemblyResolver;

        // Create writer parameters
        WriterParameters writerParameters = new WriterParameters(); 

//      // Process any assemblies which need it
        foreach (String assemblyPath in assemblyPaths) {
            if (assemblyPath.Contains("Mono.Cecil") || assemblyPath.Contains("firstpass")) {
                Debug.Log("Skipping: " + assemblyPath);
                continue;
            }

            // mdbs have the naming convention myDll.dll.mdb whereas pdbs have myDll.pdb
            String mdbPath = assemblyPath + ".mdb";
            String pdbPath = assemblyPath.Substring(0, assemblyPath.Length - 3) + "pdb";

            // Figure out if there's an pdb/mdb to go with it
            if (File.Exists(pdbPath)) {
                readerParameters.ReadSymbols = true;
                readerParameters.SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider();
                writerParameters.WriteSymbols = true;
                writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider(); // pdb written out as mdb, as mono can't work with pdbs
            } else if (File.Exists(mdbPath)) {
                readerParameters.ReadSymbols = true;
                readerParameters.SymbolReaderProvider = new Mono.Cecil.Mdb.MdbReaderProvider();
                writerParameters.WriteSymbols = true;
                writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider();
            } else {
                readerParameters.ReadSymbols = false;
                readerParameters.SymbolReaderProvider = null;
                writerParameters.WriteSymbols = false;
                writerParameters.SymbolWriterProvider = null;
            }

            // Read assembly
            AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readerParameters);

            // Process it if it hasn't already
            Debug.Log("Processing " + Path.GetFileName(assemblyPath) + "...");

            try {
                ProcessAssembly(assembly);
            }
            catch (Exception e) {
                // Skip writing if any exception occurred
                Debug.LogError("Exception while processing assembly: " + assembly.FullName + ", " + e.Message);
                continue;
            }

            Debug.Log("Writing to " + assemblyPath + "...");
            assembly.Write(assemblyPath, writerParameters);
        }

        // Unlock now that we're done
        EditorApplication.UnlockReloadAssemblies();
    }

    private static void ProcessAssembly(AssemblyDefinition assembly) {
        foreach (ModuleDefinition module in assembly.Modules) {
            MethodReference attributeConstructor = module.Import(
                typeof(RamjetProfilerPostProcessedAssemblyAttribute).GetConstructor(Type.EmptyTypes));
            var attribute = new CustomAttribute(attributeConstructor);
            if (module.HasCustomAttributes && module.CustomAttributes.Contains(attribute)) {
                Debug.Log("Skipping already-patched module:  " + module.Name);
                continue;
            }
            module.CustomAttributes.Add(attribute);

            foreach (TypeDefinition type in module.Types) {
                // Skip any classes related to the RamjetProfiler
                if (type.Name.Contains("AssemblyPostProcessor") || type.Name.Contains("RamjetProfiler")) { // Todo: use actual type equals, not string matching
                    Debug.Log("Skipping self class : " + type.Name);
                    continue;
                }

                foreach (MethodDefinition method in type.Methods) {
                    MethodReference beginMethod =
                        module.Import(typeof (RamjetProfiler).GetMethod("BeginSample",
                            BindingFlags.Static | BindingFlags.Public));
                    MethodReference endMethod =
                        module.Import(typeof (RamjetProfiler).GetMethod("EndSample",
                            BindingFlags.Static | BindingFlags.Public));

                    ILProcessor ilProcessor = method.Body.GetILProcessor();

                    Instruction first = method.Body.Instructions[0];
                    ilProcessor.InsertBefore(first,
                        Instruction.Create(OpCodes.Ldstr, type.FullName + "." + method.Name));
                    ilProcessor.InsertBefore(first, Instruction.Create(OpCodes.Call, beginMethod));

                    Instruction last = method.Body.Instructions[method.Body.Instructions.Count - 1];
                    ilProcessor.InsertBefore(last, Instruction.Create(OpCodes.Call, endMethod));
                }
            }  
        }
    }
}

/// <summary>
/// Used to mark modules in assemblies as already patched.
/// </summary>
[AttributeUsage(AttributeTargets.Module)]
public class RamjetProfilerPostProcessedAssemblyAttribute: Attribute {
}