using System.Reflection;

namespace Immerse.BfhClient.EditTests
{
	public static class ReflectionExtensions {
		public static T GetFieldValue<T>(this object obj, string name) {
			// Set the flags so that private and public fields from instances will be found
			const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			var field = obj.GetType().GetField(name, bindingFlags);
			return (T)field?.GetValue(obj);
		}
	}
}