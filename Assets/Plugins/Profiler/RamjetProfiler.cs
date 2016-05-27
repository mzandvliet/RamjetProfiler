using System.Diagnostics;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;

/* Todo:
 * 
 * How do we ensure we can Clear() at the start of a frame and LogResults() at the end of it?
 * 
 * How do we control how deep the stack goes? We want to do this as an optimization. Do we
 * decide this at the runtime profiler stack level, or at the compile time code inject level? Both?
 * 
 * While you can keep the ProfilerEntry tree structure for multiple frames, not every function from
 * one frame will be called in the next frame. So if a function has no results, don't log it.
 * 
 * Conditional participation in build phase, like the default Unity profiler.
 * 
 * Begin/End Sample functions should be reaaaaally fast.
 */

public static class RamjetProfiler {
	private static Stack<ProfilerEntry> _stack;
	private static ProfilerEntry _root;

    static RamjetProfiler() {
		_root = new ProfilerEntry ("Game");
		_stack = new Stack<ProfilerEntry> ();
		_stack.Push (_root);
    }

    public static void BeginSample(string name) {
		var child = _stack.Peek ().GetOrCreateChild (name);
		child.Start ();
		_stack.Push (child);
    }

    public static void EndSample() {
		var child = _stack.Pop ();
		child.Stop ();
    }

	public static void LogFrameResults() {
		string results = "Profiler Results:\n";

		while (_stack.Count != 0) {
			var child = _stack.Pop ();

			if (child.HasResult ()) {
				// Print function results with indentation
				for (int i = 0; i < _stack.Count; i++) {
					results += "  ";
				}
				results += string.Format("- {0} : {1:0:00}ms\n", child.Id, child.GetResult() * 1000d);
			}

			for (int i = 0; i < child.Children.Count; i++) {
				_stack.Push (child.Children [i]);
			}
		}

		Debug.Log (results);
	}

	public static void ClearFrameResults() {
		while (_stack.Count != 0) {
			var child = _stack.Pop ();
			child.Clear ();
			for (int i = 0; i < child.Children.Count; i++) {
				_stack.Push (child.Children [i]);
			}
		}
		_stack.Push (_root);
	}
}

/* Todo: 
 * 
 * Needs to be easily serializable
 * 
 * Can improve lookup / iteration by having a list container of ProfilerEntries with numeric ids, and a dictionary
 * that maps a string ID to a numerical entry */ 

public class ProfilerEntry {
	private string _id;
	private IList<ProfilerEntry> _children;
	private IList<double> _results;
	private Stopwatch _stopwatch;

	public string Id {
		get { return _id; }
	}

	public IList<ProfilerEntry> Children {
		get { return _children; }
	}

	public ProfilerEntry(string id) {
		_id = id;
		_children = new List<ProfilerEntry> ();
		_results = new List<double> ();
		_stopwatch = new Stopwatch ();
	}

	public void Start() {
		_stopwatch.Reset ();
		_stopwatch.Start ();
	}

	public void Stop() {
		_stopwatch.Stop ();
		_results.Add (_stopwatch.Elapsed.Seconds);
	}

	public bool HasResult() {
		return _results.Count != 0;
	}

	public double GetResult() {
		if (_children.Count == 0) {
			return -1d;
		}

		double average = 0f;
		for (int i = 0; i < _results.Count; i++) {
			average += _results [i];
		}
		return average / (double)_results.Count;
	}

	public void Clear() {
		_results.Clear ();
	}

	public ProfilerEntry GetOrCreateChild(string id) {
		int index = GetChildIndex (id);
		if (index == -1) {
			_children.Add (new ProfilerEntry (id));
			index = _children.Count - 1;
		}

		return _children [index];
	}

	public int GetChildIndex(string id) {
		for (int i = 0; i < _children.Count; i++) {
			if (_children [i].Id.Equals (id)) {
				return i;
			}
		}
		return -1;
	}
}
