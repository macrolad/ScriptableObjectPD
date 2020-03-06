# ScriptableObjectPD
Property drawer that makes the use of ScriptableObjects more convenient:

- #### Show its fields in the inspector.
The label becomes a foldout that shows and hides it's serializable fields, just like a regular C# class.

- #### Fast asset creation.
Allows the creation of ScriptableObject assets from the inspector. Select the type from the provided dropdown and save it where you want.

## How to use
- Create a new script.
- Add the following code:

		using UnityEditor;

		namespace Utilities.ScriptableFactory
		{
				[CustomPropertyDrawer(typeof(T))]
				public class MyScript : ScriptableFactory<T> {}
		}
- Change T to a class that inherits from ScriptableObject.
