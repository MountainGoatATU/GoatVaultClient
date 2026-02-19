namespace GoatVaultCore.Services.Shamir
{
    /// <summary>
    /// Converts between raw byte arrays and human-readable mnemonic word sequences.
    /// </summary>
    public interface IMnemonicEncoder
    {
        string Encode(byte[] data);
        byte[] Decode(string mnemonic);
    }
    public sealed class Bip39MnemonicEncoder : IMnemonicEncoder
    {
        private readonly string[] _wordList = [];
        private readonly Dictionary<string, int> _wordIndex;

        private const int BitsPerWord = 11;
        private const int WordListSize = 2048;

        public Bip39MnemonicEncoder(string[] wordList)
        {
            _wordList = wordList;
            if (_wordList.Length != WordListSize)
                throw new ArgumentException($"Word list must contain exactly {WordListSize} words.");

            _wordIndex = new Dictionary<string, int>(WordListSize, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < wordList.Length; i++)
            {
                _wordIndex[wordList[i]] = i;
            }
        }

        public string Encode(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var payload = new byte[data.Length + 1];
            payload[0] = (byte)data.Length;
            Buffer.BlockCopy(data, 0, payload, 1, data.Length);

            var totalBits = payload.Length * 8;
            var wordCount = (totalBits + BitsPerWord - 1) / BitsPerWord;
            var words = new string[wordCount];

            for (var i = 0; i < wordCount; i++)
            {
                var bitOffset = i * BitsPerWord;
                var value = 0;

                for (var bit = 0; bit < BitsPerWord; bit++)
                {
                    var currentBit = bitOffset + bit;
                    var byteIdx = currentBit / 8;
                    var bitIdx = 7 - (currentBit % 8);

                    if (byteIdx < payload.Length && (payload[byteIdx] & (1 << bitIdx)) != 0)
                    {
                        value |= (1 << (BitsPerWord - 1 - bit));
                    }
                }
                words[i] = _wordList[value];
            }

            return string.Join(' ', words);
        }

        public byte[] Decode(string mnemonic)
        {
            ArgumentNullException.ThrowIfNull(mnemonic);

            var words = mnemonic.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var totalBits = words.Length * BitsPerWord;
            var buffer = new byte[(totalBits+7)/8];

            for (var i = 0; i < words.Length; i++)
            {
               if (!_wordIndex.TryGetValue(words[i], out var wordValue))
                    throw new FormatException($"Unknown word in mnemonic: {words[i]}");

               var bitOffset = i * BitsPerWord;

               for (var bit = 0; bit < BitsPerWord; bit++)
               {
                   if ((wordValue & (1 << (BitsPerWord - 1 - bit))) == 0) 
                       continue;

                   var currentBit = bitOffset + bit;
                   var byteIdx = currentBit / 8;
                   var bitIdx = 7 - (currentBit % 8);

                   if (byteIdx < buffer.Length)
                       buffer[byteIdx] |= (byte)(1 << bitIdx);
               }
            }

            if (buffer.Length == 0) throw new FormatException("Decoded data is empty.");

            int originalLength = buffer[0];
            if (originalLength > buffer.Length - 1)
                throw new FormatException("Invalid original length in decoded data.");

            var result = new byte[originalLength];
            Buffer.BlockCopy(buffer, 1, result, 0, originalLength);
            return result;
        }
    }
}
