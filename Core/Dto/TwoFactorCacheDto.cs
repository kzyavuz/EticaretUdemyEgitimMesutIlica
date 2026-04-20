using System;
using System.Collections.Generic;
using System.Text;

namespace Core.Dto
{
    public class TwoFactorCacheDto
    {
        public string Code { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        public bool RememberMe { get; set; }
    }
}
