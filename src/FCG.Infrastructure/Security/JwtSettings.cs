namespace FCG.Infrastructure.Security;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 4;

    /// <summary>
    /// Emissor do token (claim "iss"). Nao e validado por esta API nem pelo
    /// catalog-api (ambos usam ValidateIssuer = false), mas o API Gateway (Kong)
    /// depende dele: o plugin jwt usa a claim "iss" para descobrir qual
    /// credencial usar na conferencia da assinatura. Sem "iss" no token, o
    /// gateway responde 401 "No mandatory 'iss' in claims".
    /// </summary>
    public string Issuer { get; set; } = "FCG";
}