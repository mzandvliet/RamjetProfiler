using System;
using UnityEngine;
using System.Collections;

public class MyComponent : MonoBehaviour {
    private void Update() {
        int j = 0;
        for (int i = 0; i < 1000000; i++) {
            j++;
        }
        MyNestedFunction();
    }

    private void MyNestedFunction() {
        int j = 0;
        for (int i = 0; i < 2000000; i++) {
            j++;
        }
    }
}
