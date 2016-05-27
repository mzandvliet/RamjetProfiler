using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
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
 * 
 * Track num invocations
 */

public static class RamjetProfiler {
	private static Stack<ProfilerEntry> _stack;
	private static ProfilerEntry _root;

    static RamjetProfiler() {
		_root = new ProfilerEntry ("Game");
		_stack = new Stack<ProfilerEntry> ();
		
        ResetStack();
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
        StringBuilder b = new StringBuilder(2048);
	    b.Append("Profiler Results:\n");
        LogFrameResultsRecursively(_root, 0, b);
		Debug.Log (b.ToString());
	}

    private static void LogFrameResultsRecursively(ProfilerEntry e, int depth, StringBuilder b) {
        if (e.HasResult()) {
            // Indentation for callstack depth
            for (int i = 0; i < depth; i++) {
                b.Append("-");
            }
            b.AppendFormat(" {0}: {1:0:00}ms, {2} calls\n", e.Id, e.AverageSeconds * 1000d, e.TimesCalled);
        }
        if (e.HasChildren()) {
            for (int i = 0; i < e.Children.Count; i++) {
                LogFrameResultsRecursively(e.Children[i], depth + 1, b);
            }
        }
    }

    public static void ClearFrameResults() {
        ResetStack();

        while (_stack.Count != 0) {
			var child = _stack.Pop ();
			child.Clear ();
			for (int i = 0; i < child.Children.Count; i++) {
				_stack.Push (child.Children [i]);
			}
		}
		_stack.Push (_root);
	}

    private static void ResetStack() {
        _stack.Clear();
        _stack.Push(_root);
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
	private readonly IList<ProfilerEntry> _children;
	private readonly Stopwatch _stopwatch;
    private ushort _timesCalled;

    public string Id {
		get { return _id; }
	}

	public IList<ProfilerEntry> Children {
		get { return _children; }
	}

    public ushort TimesCalled {
        get { return _timesCalled; }
    }

    public double TotalSeconds {
        get { return _stopwatch.Elapsed.TotalSeconds; }
    }

    public double AverageSeconds {
        get { return _stopwatch.Elapsed.TotalSeconds/(double) _timesCalled; }
    }

    public ProfilerEntry(string id) {
		_id = id;
		_children = new List<ProfilerEntry> ();
		_stopwatch = new Stopwatch ();
	}

	public void Start() {
	    _timesCalled++;
        _stopwatch.Start();
	}

	public void Stop() {
        _stopwatch.Stop ();
    }

    public bool HasChildren() {
        return _children.Count > 0;
    }

	public bool HasResult() {
	    return _timesCalled > 0;
	}

	public void Clear() {
	    _timesCalled = 0;
		_stopwatch.Reset();
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
