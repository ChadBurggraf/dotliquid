using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DotLiquid.Exceptions;
using DotLiquid.Util;

namespace DotLiquid.Tags
{
    /// <summary>
    /// Assign sets a variable in your template.
    /// 
    /// {% assign foo = 'monkey' %}
    /// 
    /// You can then use the variable later in the page.
    /// 
    /// {{ foo }}
    /// </summary>
    public class Assign : Tag
    {
        private static readonly Regex Syntax = R.B(R.Q(@"({0}+)\s*=\s*({1}+)"), Liquid.VariableSignature, Liquid.QuotedFragment);

        private string _to, _from;

        public override void Initialize(string tagName, string markup, List<string> tokens)
        {
            Match syntaxMatch = Syntax.Match(markup);
            if (syntaxMatch.Success)
            {
                _to = syntaxMatch.Groups[1].Value;
                _from = syntaxMatch.Groups[2].Value;
            }
            else
            {
                throw new SyntaxException("Syntax Error in 'assign' - Valid syntax: assign [var] = [source]");
            }

            base.Initialize(tagName, markup, tokens);
        }

        public override void Render(Context context, StringBuilder result)
        {
            context.Scopes.Last()[_to] = context[_from];
        }
    }
}