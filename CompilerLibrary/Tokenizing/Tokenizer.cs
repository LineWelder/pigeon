using System.IO;

namespace CompilerLibrary.Tokenizing
{
    public class Tokenizer
    {
        public StreamReader stream;

        public Tokenizer(StreamReader stream)
        {
            this.stream = stream;
        }

        public string NextToken()
        {
            return stream.ReadLine();
        }
    }
}
