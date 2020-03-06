using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Utilities.ScriptableFactory
{
    [CustomPropertyDrawer(typeof(Animal))]
    public class AnimalSOF : ScriptableFactory<Animal>
    {
    }
}
