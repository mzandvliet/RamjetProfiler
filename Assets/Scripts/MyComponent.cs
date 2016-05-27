using UnityEngine;
using System.Threading;

public class MyComponent : MonoBehaviour {
    private void Update() {
        MyNestedFunctionA();
        MyNestedFunctionB();
    }

    private void MyNestedFunctionA() {
        Thread.Sleep(5);
    }

    private void MyNestedFunctionB() {
        Thread.Sleep(4);
        for (int i = 0; i < 5; i++) {
            MyNestedFunctionC();
        }
    }

    private void MyNestedFunctionC() {
        Thread.Sleep(1);
    }
}
