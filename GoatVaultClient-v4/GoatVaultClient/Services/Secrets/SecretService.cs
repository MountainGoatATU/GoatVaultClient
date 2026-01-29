using System.Numerics;
using System.Text;
using SecretSharingDotNet.Cryptography;
using SecretSharingDotNet.Math;

namespace GoatVaultClient.Services;

public class SecretService(
    IMakeSharesUseCase<BigInteger> makeSharesUseCase,
    IReconstructionUseCase<BigInteger> reconstructionUseCase)
{
    private readonly IMakeSharesUseCase<BigInteger> _makeSharesUseCase = makeSharesUseCase;
    private readonly IReconstructionUseCase<BigInteger> _reconstructionUseCase = reconstructionUseCase;

    public List<string> CreateSecret(int threshold, int totalShares, string phrase)
    {
        try
        {
            var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();

            // TODO: Create Shamir's Secret Sharing instance with BigInteger
            // var split = new ShamirsSecretSharing<BigInteger>(gcd);
            // var shares = split.MakeShares(threshold, totalShares, phrase);
            // var shareStrings = shares.Select(s => s.ToString()).ToList();
            // return shareStrings;

            // TODO: Temporary return
            return [];

        }
        catch (Exception ex)
        {
            throw new Exception("Error creating secret shares: " + ex.Message);
        }
    }

    public string ReconstructSecret(string[] shareStrings)
    {
        try
        {
            var gcd = new ExtendedEuclideanAlgorithm<BigInteger>();

            // TODO: ShamirSecretSharing
            // var combine = new ShamirsSecretSharing<BigInteger>(gcd);

            // TODO: Obsolete
            var reconstructedSecret = _reconstructionUseCase.Reconstruction(shareStrings);

            var bytes = reconstructedSecret.ToByteArray();
            var tempString = Encoding.UTF8.GetString(bytes);

            var originalString = tempString.Replace("\0", "");

            return originalString;
        }
        catch (Exception ex)
        {
            throw new Exception("Error reconstructing secret: " + ex.Message);
        }
    }
}
