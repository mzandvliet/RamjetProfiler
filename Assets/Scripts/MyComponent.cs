using System;
using UnityEngine;
using System.Collections;
using System.Threading;

public class MyComponent : MonoBehaviour {
    private void Update() {
        MyNestedFunctionA();
        MyNestedFunctionB();
    }

    private void MyNestedFunctionA() {
        Thread.Sleep(6);
    }

    private void MyNestedFunctionB() {
        Thread.Sleep(10);
    }
}
