using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DisposalManager
	{
		private List<Object> disposeObjects;

		public DisposalManager() => disposeObjects = new List<Object>();

		public void DisposeAll()
		{
			if (disposeObjects == null)
				return;
			var c = disposeObjects.Count;
			for (var k = 0; k < c; k++)
			{
				var o = disposeObjects[k];
				if (o != null)
					Object.DestroyImmediate(o);
			}
			disposeObjects.Clear();
		}

		public void MarkForDisposal(Object o)
		{
			if (o == null)
				return;
			o.hideFlags |= HideFlags.DontSaveInEditor;
			disposeObjects.Add(o);
		}
	}
}