using System;
using System.Collections.Generic;
using System.Text;

namespace BaldiTexturePacks
{
    internal class TexturePackLoadException : Exception
    {
        private string _message;
        private TexturePack _pack;
        public TexturePackLoadException(TexturePack pack, string message)
        {
            _message = message;
            _pack = pack;
        }

        public override string Message => String.Format("({0}) {1}", _pack.Name, _message);
    }
}
