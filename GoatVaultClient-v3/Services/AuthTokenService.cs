using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoatVaultClient_v3.Services
{
    public interface IAuthTokenService
    {
        string? GetToken();
        void SetToken(string token);
        void ClearToken();
    }
    public class AuthTokenService : IAuthTokenService
    {
        private string? _token;

        public string? GetToken()
        {
            return _token;
        }

        public void SetToken(string token)
        {
            _token = token;
        }

        public void ClearToken()
        {
            _token = null;
        }
    }
}
