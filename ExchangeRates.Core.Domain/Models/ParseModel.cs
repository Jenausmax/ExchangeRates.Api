namespace ExchangeRates.Core.Domain.Models
{
    public class ParseModel
    {
        public string Id { get; set; }
        public string NumCode { get; set; }
        public string CharCode { get; set; }
        public int Nominal { get; set; }
        public string Name { get; set; }
        public double Value { get; set; }
        public double Previous { get; set; }
    }

    #region Valute class

    public class AUD : ParseModel { }

    public class AZN : ParseModel { }

    public class GBP : ParseModel { }

    public class AMD : ParseModel { }

    public class BYN : ParseModel { }

    public class BGN : ParseModel { }

    public class BRL : ParseModel { }

    public class HUF : ParseModel { }

    public class HKD : ParseModel { }

    public class DKK : ParseModel { }

    public class USD : ParseModel { }

    public class EUR : ParseModel { }

    public class INR : ParseModel { }

    public class KZT : ParseModel { }

    public class CAD : ParseModel { }

    public class KGS : ParseModel { }

    public class CNY : ParseModel { }

    public class MDL : ParseModel { }

    public class NOK : ParseModel { }

    public class PLN : ParseModel { }

    public class RON : ParseModel { }

    public class XDR : ParseModel { }

    public class SGD : ParseModel { }

    public class TJS : ParseModel { }

    public class TRY : ParseModel { }

    public class TMT : ParseModel { }

    public class UZS : ParseModel { }

    public class UAH : ParseModel { }

    public class CZK : ParseModel { }

    public class SEK : ParseModel { }

    public class CHF : ParseModel { }

    public class ZAR : ParseModel { }

    public class KRW : ParseModel { }

    public class JPY : ParseModel { }


    #endregion

}
