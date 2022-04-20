using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class MapTests
{
    private GameObject _gameObject;
    private static readonly int Bitmap = Shader.PropertyToID("Bitmap");

    [Test]
    public void MapTestsSimplePasses()
    {
        var renderer = _gameObject.GetComponent<SpriteRenderer>();
        renderer.material.GetTexture(Bitmap);
        
    }

    [UnityTest]
    public IEnumerator MapTestsWithEnumeratorPasses()
    {
        yield return null;
    }
}
