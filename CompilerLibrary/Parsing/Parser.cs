using CompilerLibrary.Tokenizing;

namespace CompilerLibrary.Parsing
{
    /// <summary>
    /// Is used for syntax tree construction
    /// </summary>
    public class Parser
    {
        private Tokenizer tokenizer;

        public Parser(Tokenizer tokenizer)
        {
            this.tokenizer = tokenizer;
        }

        /// <summary>
        /// Parses a type
        /// </summary>
        /// <returns>The parsed node</returns>
        private SyntaxNode ParseType()
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Parses a single declaration
        /// </summary>
        /// <returns>The parsed node</returns>
        public SyntaxNode Parse()
        {
            throw new System.NotImplementedException();
        }
    }
}
