using ExchangeRates.Core.Domain.Interfaces;
using ExchangeRates.Core.Domain.Models;
using ExchangeRates.Infrastructure.DB.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ExchangeRates.Core.App.Services
{
    public class SaveService : ISaveService
    {
        private readonly ILogger _logger;
        private readonly IRepositoryBase<ValuteModelDb> _repository;

        public SaveService(ILogger logger, IRepositoryBase<ValuteModelDb> repository)
        {
            _logger = logger;
            _repository = repository;
        }
        public async Task<bool> SaveSet(Root item, CancellationToken cancel)
        {
            if (item != null)
            {
                var date = item.Date;
                var stampDate = item.Timestamp;
                var dateSave = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                DateTime d = default;
                DateTime.TryParse(dateSave, out d);
                var valutes = item.Valute;
                var listValute = new List<ValuteModelDb>();

                var amd = new ValuteModelDb()
                {
                    NumCode = valutes.AMD.NumCode,
                    CharCode = valutes.AMD.CharCode,
                    Nominal = valutes.AMD.Nominal,
                    Name = valutes.AMD.Name,
                    Value = valutes.AMD.Value,
                    Previous = valutes.AMD.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.AMD.Id
                };
                listValute.Add(amd);

                var aud = new ValuteModelDb()
                {
                    NumCode = valutes.AUD.NumCode,
                    CharCode = valutes.AUD.CharCode,
                    Nominal = valutes.AUD.Nominal,
                    Name = valutes.AUD.Name,
                    Value = valutes.AUD.Value,
                    Previous = valutes.AUD.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.AUD.Id
                };
                listValute.Add(aud);

                var azn = new ValuteModelDb()
                {
                    NumCode = valutes.AZN.NumCode,
                    CharCode = valutes.AZN.CharCode,
                    Nominal = valutes.AZN.Nominal,
                    Name = valutes.AZN.Name,
                    Value = valutes.AZN.Value,
                    Previous = valutes.AZN.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.AZN.Id
                };
                listValute.Add(azn);

                var bgn = new ValuteModelDb()
                {
                    NumCode = valutes.BGN.NumCode,
                    CharCode = valutes.BGN.CharCode,
                    Nominal = valutes.BGN.Nominal,
                    Name = valutes.BGN.Name,
                    Value = valutes.BGN.Value,
                    Previous = valutes.BGN.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.BGN.Id
                };
                listValute.Add(bgn);

                var brl = new ValuteModelDb()
                {
                    NumCode = valutes.BRL.NumCode,
                    CharCode = valutes.BRL.CharCode,
                    Nominal = valutes.BRL.Nominal,
                    Name = valutes.BRL.Name,
                    Value = valutes.BRL.Value,
                    Previous = valutes.BRL.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.BRL.Id
                };
                listValute.Add(brl);

                var byn = new ValuteModelDb()
                {
                    NumCode = valutes.BYN.NumCode,
                    CharCode = valutes.BYN.CharCode,
                    Nominal = valutes.BYN.Nominal,
                    Name = valutes.BYN.Name,
                    Value = valutes.BYN.Value,
                    Previous = valutes.BYN.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.BYN.Id
                };
                listValute.Add(byn);

                var cad = new ValuteModelDb()
                {
                    NumCode = valutes.CAD.NumCode,
                    CharCode = valutes.CAD.CharCode,
                    Nominal = valutes.CAD.Nominal,
                    Name = valutes.CAD.Name,
                    Value = valutes.CAD.Value,
                    Previous = valutes.CAD.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.CAD.Id
                };
                listValute.Add(cad);

                var chf = new ValuteModelDb()
                {
                    NumCode = valutes.CHF.NumCode,
                    CharCode = valutes.CHF.CharCode,
                    Nominal = valutes.CHF.Nominal,
                    Name = valutes.CHF.Name,
                    Value = valutes.CHF.Value,
                    Previous = valutes.CHF.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.CHF.Id
                };
                listValute.Add(chf);

                var cny = new ValuteModelDb()
                {
                    NumCode = valutes.CNY.NumCode,
                    CharCode = valutes.CNY.CharCode,
                    Nominal = valutes.CNY.Nominal,
                    Name = valutes.CNY.Name,
                    Value = valutes.CNY.Value,
                    Previous = valutes.CNY.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.CNY.Id
                };
                listValute.Add(cny);

                var czk = new ValuteModelDb()
                {
                    NumCode = valutes.CZK.NumCode,
                    CharCode = valutes.CZK.CharCode,
                    Nominal = valutes.CZK.Nominal,
                    Name = valutes.CZK.Name,
                    Value = valutes.CZK.Value,
                    Previous = valutes.CZK.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.CZK.Id
                };
                listValute.Add(czk);

                var dkk = new ValuteModelDb()
                {
                    NumCode = valutes.DKK.NumCode,
                    CharCode = valutes.DKK.CharCode,
                    Nominal = valutes.DKK.Nominal,
                    Name = valutes.DKK.Name,
                    Value = valutes.DKK.Value,
                    Previous = valutes.DKK.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.DKK.Id
                };
                listValute.Add(dkk);

                var eur = new ValuteModelDb()
                {
                    NumCode = valutes.EUR.NumCode,
                    CharCode = valutes.EUR.CharCode,
                    Nominal = valutes.EUR.Nominal,
                    Name = valutes.EUR.Name,
                    Value = valutes.EUR.Value,
                    Previous = valutes.EUR.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.EUR.Id
                };
                listValute.Add(eur);

                var gbp = new ValuteModelDb()
                {
                    NumCode = valutes.GBP.NumCode,
                    CharCode = valutes.GBP.CharCode,
                    Nominal = valutes.GBP.Nominal,
                    Name = valutes.GBP.Name,
                    Value = valutes.GBP.Value,
                    Previous = valutes.GBP.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.GBP.Id
                };
                listValute.Add(gbp);

                var hkd = new ValuteModelDb()
                {
                    NumCode = valutes.HKD.NumCode,
                    CharCode = valutes.HKD.CharCode,
                    Nominal = valutes.HKD.Nominal,
                    Name = valutes.HKD.Name,
                    Value = valutes.HKD.Value,
                    Previous = valutes.HKD.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.HKD.Id
                };
                listValute.Add(hkd);

                var huf = new ValuteModelDb()
                {
                    NumCode = valutes.HUF.NumCode,
                    CharCode = valutes.HUF.CharCode,
                    Nominal = valutes.HUF.Nominal,
                    Name = valutes.HUF.Name,
                    Value = valutes.HUF.Value,
                    Previous = valutes.HUF.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.HUF.Id
                };
                listValute.Add(huf);

                var inr = new ValuteModelDb()
                {
                    NumCode = valutes.INR.NumCode,
                    CharCode = valutes.INR.CharCode,
                    Nominal = valutes.INR.Nominal,
                    Name = valutes.INR.Name,
                    Value = valutes.INR.Value,
                    Previous = valutes.INR.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.INR.Id
                };
                listValute.Add(inr);

                var jpy = new ValuteModelDb()
                {
                    NumCode = valutes.JPY.NumCode,
                    CharCode = valutes.JPY.CharCode,
                    Nominal = valutes.JPY.Nominal,
                    Name = valutes.JPY.Name,
                    Value = valutes.JPY.Value,
                    Previous = valutes.JPY.Previous,
                   DateValute = date,
                   TimeStampUpdateValute = stampDate,
                   DateSave = d,
                   ValuteId = valutes.JPY.Id
                };
                listValute.Add(jpy);

                var kgs = new ValuteModelDb()
                {
                    NumCode = valutes.KGS.NumCode,
                    CharCode = valutes.KGS.CharCode,
                    Nominal = valutes.KGS.Nominal,
                    Name = valutes.KGS.Name,
                    Value = valutes.KGS.Value,
                    Previous = valutes.KGS.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.KGS.Id
                };
                listValute.Add(kgs);

                var krw = new ValuteModelDb()
                {
                    NumCode = valutes.KRW.NumCode,
                    CharCode = valutes.KRW.CharCode,
                    Nominal = valutes.KRW.Nominal,
                    Name = valutes.KRW.Name,
                    Value = valutes.KRW.Value,
                    Previous = valutes.KRW.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.KRW.Id
                };
                listValute.Add(krw);

                var kzt = new ValuteModelDb()
                {
                    NumCode = valutes.KZT.NumCode,
                    CharCode = valutes.KZT.CharCode,
                    Nominal = valutes.KZT.Nominal,
                    Name = valutes.KZT.Name,
                    Value = valutes.KZT.Value,
                    Previous = valutes.KZT.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.KZT.Id
                };
                listValute.Add(kzt);

                var mdl = new ValuteModelDb()
                {
                    NumCode = valutes.MDL.NumCode,
                    CharCode = valutes.MDL.CharCode,
                    Nominal = valutes.MDL.Nominal,
                    Name = valutes.MDL.Name,
                    Value = valutes.MDL.Value,
                    Previous = valutes.MDL.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.MDL.Id
                };
                listValute.Add(mdl);

                var nok = new ValuteModelDb()
                {
                    NumCode = valutes.NOK.NumCode,
                    CharCode = valutes.NOK.CharCode,
                    Nominal = valutes.NOK.Nominal,
                    Name = valutes.NOK.Name,
                    Value = valutes.NOK.Value,
                    Previous = valutes.NOK.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.NOK.Id
                };
                listValute.Add(nok);

                var pln = new ValuteModelDb()
                {
                    NumCode = valutes.PLN.NumCode,
                    CharCode = valutes.PLN.CharCode,
                    Nominal = valutes.PLN.Nominal,
                    Name = valutes.PLN.Name,
                    Value = valutes.PLN.Value,
                    Previous = valutes.PLN.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.PLN.Id
                };
                listValute.Add(pln);

                var ron = new ValuteModelDb()
                {
                    NumCode = valutes.RON.NumCode,
                    CharCode = valutes.RON.CharCode,
                    Nominal = valutes.RON.Nominal,
                    Name = valutes.RON.Name,
                    Value = valutes.RON.Value,
                    Previous = valutes.RON.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.RON.Id
                };
                listValute.Add(ron);

                var sek = new ValuteModelDb()
                {
                    NumCode = valutes.SEK.NumCode,
                    CharCode = valutes.SEK.CharCode,
                    Nominal = valutes.SEK.Nominal,
                    Name = valutes.SEK.Name,
                    Value = valutes.SEK.Value,
                    Previous = valutes.SEK.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.SEK.Id
                };
                listValute.Add(sek);

                var sgd = new ValuteModelDb()
                {
                    NumCode = valutes.SGD.NumCode,
                    CharCode = valutes.SGD.CharCode,
                    Nominal = valutes.SGD.Nominal,
                    Name = valutes.SGD.Name,
                    Value = valutes.SGD.Value,
                    Previous = valutes.SGD.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.SGD.Id
                };
                listValute.Add(sgd);

                var tjs = new ValuteModelDb()
                {
                    NumCode = valutes.TJS.NumCode,
                    CharCode = valutes.TJS.CharCode,
                    Nominal = valutes.TJS.Nominal,
                    Name = valutes.TJS.Name,
                    Value = valutes.TJS.Value,
                    Previous = valutes.TJS.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.TJS.Id
                };
                listValute.Add(tjs);

                var tmt = new ValuteModelDb()
                {
                    NumCode = valutes.TMT.NumCode,
                    CharCode = valutes.TMT.CharCode,
                    Nominal = valutes.TMT.Nominal,
                    Name = valutes.TMT.Name,
                    Value = valutes.TMT.Value,
                    Previous = valutes.TMT.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.TMT.Id
                };
                listValute.Add(tmt);

                var tryv = new ValuteModelDb()
                {
                    NumCode = valutes.TRY.NumCode,
                    CharCode = valutes.TRY.CharCode,
                    Nominal = valutes.TRY.Nominal,
                    Name = valutes.TRY.Name,
                    Value = valutes.TRY.Value,
                    Previous = valutes.TRY.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.TRY.Id
                };
                listValute.Add(tryv);

                var uah = new ValuteModelDb()
                {
                    NumCode = valutes.UAH.NumCode,
                    CharCode = valutes.UAH.CharCode,
                    Nominal = valutes.UAH.Nominal,
                    Name = valutes.UAH.Name,
                    Value = valutes.UAH.Value,
                    Previous = valutes.UAH.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.UAH.Id
                };
                listValute.Add(uah);

                var usd = new ValuteModelDb()
                {
                    NumCode = valutes.USD.NumCode,
                    CharCode = valutes.USD.CharCode,
                    Nominal = valutes.USD.Nominal,
                    Name = valutes.USD.Name,
                    Value = valutes.USD.Value,
                    Previous = valutes.USD.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.USD.Id
                };
                listValute.Add(usd);

                var uzs = new ValuteModelDb()
                {
                    NumCode = valutes.UZS.NumCode,
                    CharCode = valutes.UZS.CharCode,
                    Nominal = valutes.UZS.Nominal,
                    Name = valutes.UZS.Name,
                    Value = valutes.UZS.Value,
                    Previous = valutes.UZS.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.UZS.Id
                };
                listValute.Add(uzs);

                var xdr = new ValuteModelDb()
                {
                    NumCode = valutes.XDR.NumCode,
                    CharCode = valutes.XDR.CharCode,
                    Nominal = valutes.XDR.Nominal,
                    Name = valutes.XDR.Name,
                    Value = valutes.XDR.Value,
                    Previous = valutes.XDR.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.XDR.Id
                };
                listValute.Add(xdr);

                var zar = new ValuteModelDb()
                {
                    NumCode = valutes.ZAR.NumCode,
                    CharCode = valutes.ZAR.CharCode,
                    Nominal = valutes.ZAR.Nominal,
                    Name = valutes.ZAR.Name,
                    Value = valutes.ZAR.Value,
                    Previous = valutes.ZAR.Previous,
                    DateValute = date,
                    TimeStampUpdateValute = stampDate,
                    DateSave = d,
                    ValuteId = valutes.ZAR.Id
                };
                listValute.Add(zar);

                _logger.Information("Начато сохранение", typeof(SaveService));
                await _repository.AddCollection(listValute, cancel);
                _logger.Information("Сохранено.", typeof(SaveService));
                return true;
            }
            else
            {
                _logger.Error("Ошибка! Root item = null", typeof(SaveService));
                return false;
            }
            
        }
    }
}
