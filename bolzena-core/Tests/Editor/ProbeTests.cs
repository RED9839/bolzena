using NUnit.Framework;
namespace Bolzena.Core.Tests { public class ProbeTests { [Test] public void RngSame() { var a = new Rng(42); var b = new Rng(42); Assert.AreEqual(a.Next(), b.Next()); } } }
