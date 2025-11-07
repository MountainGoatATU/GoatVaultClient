using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Math;

namespace GoatVaultClient_v2.Services
{
    public class SecretService
    {
        private IMakeSharesUseCase<BigInteger> _makeSharesUseCase;
        private IReconstructionUseCase<BigInteger> _reconstructionUseCase;

        public SecretService(IMakeSharesUseCase<BigInteger> makeSharesUseCase, IReconstructionUseCase<BigInteger> reconstructionUseCase)
        {
            this._makeSharesUseCase = makeSharesUseCase;
            this._reconstructionUseCase = reconstructionUseCase;
        }

        public List<string> CreateSecret(string secret, int totalShares, int threshold)
        {
            try
            {
                var shares = _makeSharesUseCase.MakeShares(threshold, totalShares, secret);

                var shareStrings = shares.Select(s => s.ToString()).ToList();

                return shareStrings;
            }
            catch (Exception ex)
            {
                throw new Exception("Error creating secret shares: " + ex.Message);
            }
        }
    }
}
