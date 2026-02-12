using System.Collections;

namespace GoatVaultCore.Services.Secrets
{
    public sealed class Bip39MnemonicEncoder : IMnemonicEncoder
    {
        private readonly string[] _wordList = [];
        private readonly Dictionary<string, int> _wordIndex = new Dictionary<string, int>();

        private const int BitsPerWord = 11;
        private const int WordListSize = 2048;

        public Bip39MnemonicEncoder(string[] wordList)
        {
            if (_wordList.Length != WordListSize)
                throw new ArgumentException($"Word list must contain exactly {WordListSize} words.");
            
            _wordList = wordList;
            _wordIndex = new Dictionary<string, int>(WordListSize, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < wordList.Length; i++)
            {
                _wordIndex[wordList[i]] = i;
            }
        }

        public string Encode(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            byte[] payload = new byte[data.Length + 1];
            payload[0] = (byte)data.Length;
            Buffer.BlockCopy(data, 0, payload, 1, data.Length);

            var bits = new BitArray(payload);
            int totalBits = bits.Length;
            int wordCount = (totalBits + BitsPerWord - 1) / BitsPerWord;

            var words = new string[wordCount];

            for (int i = 0; i < wordCount; i++)
            {
                int index = 0;
                for (int bit = 0; bit < BitsPerWord; bit++)
                {
                    int bitIndex = i * BitsPerWord + bit;
                    if (bitIndex < totalBits && bits[bitIndex])
                    {
                        index |= (1 << (BitsPerWord - 1 - bit));
                    }
                }
                words[i] = _wordList[index];
            }
            return string.Join(' ', words);
        }
        public byte[] Decode(string mnemonic)
        {
            ArgumentNullException.ThrowIfNull(mnemonic);

            string[] words = mnemonic.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            int totalBits = words.Length * BitsPerWord;
            var bits = new BitArray(totalBits);

            for (int i = 0; i < words.Length; i++)
            {
                if (!_wordIndex.TryGetValue(words[i], out int index))
                    throw new FormatException($"Unknown word in mnemonic: {words[i]}");

                for (int bit = 0; bit < BitsPerWord; bit++)
                {
                    if ((index & (1 << (BitsPerWord - 1 - bit))) !=0)
                    {
                        bits[i * BitsPerWord + bit] = true;
                    }
                }
            }

            // Convert bits back to bytes
            int totalBytes = totalBits / 8;
            byte[] allBytes = new byte[totalBytes];
            bits.CopyTo(allBytes, 0);

            // First byte is the length of the original data
            int originalLength = allBytes[0];
            byte[] result = new byte[originalLength];
            Buffer.BlockCopy(allBytes, 1, result, 0, originalLength);

            return result;
        }
    }
}
