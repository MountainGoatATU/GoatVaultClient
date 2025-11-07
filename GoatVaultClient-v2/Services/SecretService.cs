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

        public List<string> CreateSecret(string phrase, int totalShares, int threshold)
        {
            try
            {
                var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();

                //// Create Shamir's Secret Sharing instance with BigInteger
                var split = new ShamirsSecretSharing<BigInteger>(gcd);

                var shares = split.MakeShares(threshold, totalShares, phrase);

                var shareStrings = shares.Select(s => s.ToString()).ToList();

                return shareStrings;

            }
            catch (Exception ex)
            {
                throw new Exception("Error creating secret shares: " + ex.Message);
            }
        }

        public BigInteger ReconstructSecret(string[] shareStrings)
        {
            try
            {
                var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();

                var combine = new ShamirsSecretSharing<BigInteger>(gcd);

                var recoveredSecret = _reconstructionUseCase.Reconstruction(shareStrings);

                return recoveredSecret;
            }
            catch (Exception ex)
            {
                throw new Exception("Error reconstructing secret: " + ex.Message);
            }
        }
    }
}
