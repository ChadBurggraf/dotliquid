using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace DotLiquid.Tests
{
	[TestFixture]
	public class DropTests
	{
		#region Classes used in tests

		internal class ContextDrop : Drop
		{
			public int Scopes
			{
				get { return Context.Scopes.Count; }
			}

			public IEnumerable<int> ScopesAsArray
			{
				get { return Enumerable.Range(1, Context.Scopes.Count); }
			}

			public int LoopPos
			{
				get { return (int) Context["forloop.index"]; }
			}

			public void Break()
			{
				Debugger.Break();
			}

			public override object BeforeMethod(string method)
			{
				return Context[method];
			}
		}

		internal class ProductDrop : Drop
		{
			internal class TextDrop : Drop
			{
				public string[] Array
				{
					get { return new[] { "text1", "text2" }; }
				}

				public string Text
				{
					get { return "text1"; }
				}
			}

			internal class CatchallDrop : Drop
			{
				public override object BeforeMethod(string method)
				{
					return "method: " + method;
				}
			}

			public TextDrop Texts()
			{
				return new TextDrop();
			}

			public CatchallDrop Catchall()
			{
				return new CatchallDrop();
			}

			public new ContextDrop Context
			{
				get { return new ContextDrop(); }
			}

			protected string CallMeNot()
			{
				return "protected";
			}
		}

		internal class EnumerableDrop : Drop, IEnumerable
		{
			public int Size
			{
				get { return 3; }
			}

			public IEnumerator GetEnumerator()
			{
				yield return 1;
				yield return 2;
				yield return 3;
			}
		}

		#endregion

		[Test]
		public void TestProductDrop()
		{
			Assert.DoesNotThrow(() =>
			{
				Template tpl = Template.Parse("  ");
				tpl.Render(Hash.FromAnonymousObject(new { product = new ProductDrop() }));
			});
		}

		[Test]
		public void TestTextDrop()
		{
			string output = Template.Parse(" {{ product.texts.text }} ")
				.Render(Hash.FromAnonymousObject(new { product = new ProductDrop() }));
			Assert.AreEqual(" text1 ", output);
		}

		[Test]
		public void TestTextDrop2()
		{
			string output = Template.Parse(" {{ product.catchall.unknown }} ")
				.Render(Hash.FromAnonymousObject(new { product = new ProductDrop() }));
			Assert.AreEqual(" method: unknown ", output);
		}

		[Test]
		public void TestTextArrayDrop()
		{
			string output = Template.Parse("{% for text in product.texts.array %} {{text}} {% endfor %}")
				.Render(Hash.FromAnonymousObject(new { product = new ProductDrop() }));
			Assert.AreEqual(" text1  text2 ", output);
		}

		[Test]
		public void TestContextDrop()
		{
			string output = Template.Parse(" {{ context.bar }} ")
				.Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), bar = "carrot" }));
			Assert.AreEqual(" carrot ", output);
		}

		[Test]
		public void TestNestedContextDrop()
		{
			string output = Template.Parse(" {{ product.context.foo }} ")
				.Render(Hash.FromAnonymousObject(new { product = new ProductDrop(), foo = "monkey" }));
			Assert.AreEqual(" monkey ", output);
		}

		[Test]
		public void TestProtected()
		{
			string output = Template.Parse(" {{ product.call_me_not }} ")
				.Render(Hash.FromAnonymousObject(new { product = new ProductDrop() }));
			Assert.AreEqual("  ", output);
		}

		[Test]
		public void TestScope()
		{
			Assert.AreEqual("1", Template.Parse("{{ context.scopes }}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop() })));
			Assert.AreEqual("2", Template.Parse("{%for i in dummy%}{{ context.scopes }}{%endfor%}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), dummy = new[] { 1 } })));
			Assert.AreEqual("3", Template.Parse("{%for i in dummy%}{%for i in dummy%}{{ context.scopes }}{%endfor%}{%endfor%}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), dummy = new[] { 1 } })));
		}

		[Test]
		public void TestScopeThroughProc()
		{
			Assert.AreEqual("1", Template.Parse("{{ s }}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), s = (Proc) (c => c["context.scopes"]) })));
			Assert.AreEqual("2", Template.Parse("{%for i in dummy%}{{ s }}{%endfor%}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), s = (Proc) (c => c["context.scopes"]), dummy = new[] { 1 } })));
			Assert.AreEqual("3", Template.Parse("{%for i in dummy%}{%for i in dummy%}{{ s }}{%endfor%}{%endfor%}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), s = (Proc) (c => c["context.scopes"]), dummy = new[] { 1 } })));
		}

		[Test]
		public void TestScopeWithAssigns()
		{
			Assert.AreEqual("variable", Template.Parse("{% assign a = 'variable'%}{{a}}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop()})));
			Assert.AreEqual("variable", Template.Parse("{% assign a = 'variable'%}{%for i in dummy%}{{a}}{%endfor%}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), dummy = new[] { 1 } })));
			Assert.AreEqual("test", Template.Parse("{% assign header_gif = \"test\"%}{{header_gif}}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop() })));
			Assert.AreEqual("test", Template.Parse("{% assign header_gif = 'test'%}{{header_gif}}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop() })));
		}

		[Test]
		public void TestScopeFromTags()
		{
			Assert.AreEqual("1", Template.Parse("{% for i in context.scopes_as_array %}{{i}}{% endfor %}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), dummy = new[] { 1 } })));
			Assert.AreEqual("12", Template.Parse("{%for a in dummy%}{% for i in context.scopes_as_array %}{{i}}{% endfor %}{% endfor %}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), dummy = new[] { 1 } })));
			Assert.AreEqual("123", Template.Parse("{%for a in dummy%}{%for a in dummy%}{% for i in context.scopes_as_array %}{{i}}{% endfor %}{% endfor %}{% endfor %}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), dummy = new[] { 1 } })));
		}

		[Test]
		public void TestAccessContextFromDrop()
		{
			Assert.AreEqual("123", Template.Parse("{% for a in dummy %}{{ context.loop_pos }}{% endfor %}").Render(Hash.FromAnonymousObject(new { context = new ContextDrop(), dummy = new[] { 1, 2, 3 } })));
		}

		[Test]
		public void TestEnumerableDrop()
		{
			Assert.AreEqual("123", Template.Parse("{% for c in collection %}{{c}}{% endfor %}").Render(Hash.FromAnonymousObject(new { collection = new EnumerableDrop() })));
		}

		[Test]
		public void TestEnumerableDropSize()
		{
			Assert.AreEqual("3", Template.Parse("{{collection.size}}").Render(Hash.FromAnonymousObject(new { collection = new EnumerableDrop() })));
		}
	}
}