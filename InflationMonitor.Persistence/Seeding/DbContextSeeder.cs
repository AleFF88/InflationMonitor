using InflationMonitor.Domain.Entities;

namespace InflationMonitor.Persistence.Seeding {
    public static class DbContextSeeder {
        public static void Seed(ApplicationDbContext context) {

            if (!context.InflationRates.Any()) {
                var inflationData = new[] {
                    // 2000
                    new InflationRate(2000,  1, 1.046m),
                    new InflationRate(2000,  2, 1.033m),
                    new InflationRate(2000,  3, 1.020m),
                    new InflationRate(2000,  4, 1.017m),
                    new InflationRate(2000,  5, 1.021m),
                    new InflationRate(2000,  6, 1.037m),
                    new InflationRate(2000,  7, 0.999m),
                    new InflationRate(2000,  8, 1.000m),
                    new InflationRate(2000,  9, 1.026m),
                    new InflationRate(2000, 10, 1.014m),
                    new InflationRate(2000, 11, 1.004m),
                    new InflationRate(2000, 12, 1.016m),
                    
                    // 2001
                    new InflationRate(2001,  1, 1.015m),
                    new InflationRate(2001,  2, 1.006m),
                    new InflationRate(2001,  3, 1.006m),
                    new InflationRate(2001,  4, 1.015m),
                    new InflationRate(2001,  5, 1.004m),
                    new InflationRate(2001,  6, 1.006m),
                    new InflationRate(2001,  7, 0.983m),
                    new InflationRate(2001,  8, 0.998m),
                    new InflationRate(2001,  9, 1.004m),
                    new InflationRate(2001, 10, 1.002m),
                    new InflationRate(2001, 11, 1.005m),
                    new InflationRate(2001, 12, 1.016m),
                    
                    // 2002
                    new InflationRate(2002,  1, 1.010m),
                    new InflationRate(2002,  2, 0.986m),
                    new InflationRate(2002,  3, 0.993m),
                    new InflationRate(2002,  4, 1.014m),
                    new InflationRate(2002,  5, 0.997m),
                    new InflationRate(2002,  6, 0.982m),
                    new InflationRate(2002,  7, 0.985m),
                    new InflationRate(2002,  8, 0.998m),
                    new InflationRate(2002,  9, 1.002m),
                    new InflationRate(2002, 10, 1.007m),
                    new InflationRate(2002, 11, 1.007m),
                    new InflationRate(2002, 12, 1.014m),
                    
                    // 2003
                    new InflationRate(2003,  1, 1.015m),
                    new InflationRate(2003,  2, 1.011m),
                    new InflationRate(2003,  3, 1.011m),
                    new InflationRate(2003,  4, 1.007m),
                    new InflationRate(2003,  5, 1.000m),
                    new InflationRate(2003,  6, 1.001m),
                    new InflationRate(2003,  7, 0.999m),
                    new InflationRate(2003,  8, 0.983m),
                    new InflationRate(2003,  9, 1.006m),
                    new InflationRate(2003, 10, 1.013m),
                    new InflationRate(2003, 11, 1.019m),
                    new InflationRate(2003, 12, 1.015m),
                    
                    // 2004
                    new InflationRate(2004,  1, 1.014m),
                    new InflationRate(2004,  2, 1.004m),
                    new InflationRate(2004,  3, 1.004m),
                    new InflationRate(2004,  4, 1.007m),
                    new InflationRate(2004,  5, 1.007m),
                    new InflationRate(2004,  6, 1.007m),
                    new InflationRate(2004,  7, 1.000m),
                    new InflationRate(2004,  8, 0.999m),
                    new InflationRate(2004,  9, 1.013m),
                    new InflationRate(2004, 10, 1.022m),
                    new InflationRate(2004, 11, 1.016m),
                    new InflationRate(2004, 12, 1.024m),
                    
                    // 2005
                    new InflationRate(2005,  1, 1.017m),
                    new InflationRate(2005,  2, 1.010m),
                    new InflationRate(2005,  3, 1.016m),
                    new InflationRate(2005,  4, 1.007m),
                    new InflationRate(2005,  5, 1.006m),
                    new InflationRate(2005,  6, 1.006m),
                    new InflationRate(2005,  7, 1.003m),
                    new InflationRate(2005,  8, 1.000m),
                    new InflationRate(2005,  9, 1.004m),
                    new InflationRate(2005, 10, 1.009m),
                    new InflationRate(2005, 11, 1.012m),
                    new InflationRate(2005, 12, 1.009m),
                    
                    // 2006
                    new InflationRate(2006,  1, 1.012m),
                    new InflationRate(2006,  2, 1.018m),
                    new InflationRate(2006,  3, 0.997m),
                    new InflationRate(2006,  4, 0.996m),
                    new InflationRate(2006,  5, 1.005m),
                    new InflationRate(2006,  6, 1.001m),
                    new InflationRate(2006,  7, 1.009m),
                    new InflationRate(2006,  8, 1.000m),
                    new InflationRate(2006,  9, 1.020m),
                    new InflationRate(2006, 10, 1.026m),
                    new InflationRate(2006, 11, 1.018m),
                    new InflationRate(2006, 12, 1.009m),
                    
                    // 2007
                    new InflationRate(2007,  1, 1.005m),
                    new InflationRate(2007,  2, 1.006m),
                    new InflationRate(2007,  3, 1.002m),
                    new InflationRate(2007,  4, 1.000m),
                    new InflationRate(2007,  5, 1.006m),
                    new InflationRate(2007,  6, 1.022m),
                    new InflationRate(2007,  7, 1.014m),
                    new InflationRate(2007,  8, 1.006m),
                    new InflationRate(2007,  9, 1.022m),
                    new InflationRate(2007, 10, 1.029m),
                    new InflationRate(2007, 11, 1.022m),
                    new InflationRate(2007, 12, 1.021m),
                    
                    // 2008
                    new InflationRate(2008,  1, 1.029m),
                    new InflationRate(2008,  2, 1.027m),
                    new InflationRate(2008,  3, 1.038m),
                    new InflationRate(2008,  4, 1.031m),
                    new InflationRate(2008,  5, 1.013m),
                    new InflationRate(2008,  6, 1.008m),
                    new InflationRate(2008,  7, 0.995m),
                    new InflationRate(2008,  8, 0.999m),
                    new InflationRate(2008,  9, 1.011m),
                    new InflationRate(2008, 10, 1.017m),
                    new InflationRate(2008, 11, 1.015m),
                    new InflationRate(2008, 12, 1.021m),
                    
                    // 2009
                    new InflationRate(2009,  1, 1.029m),
                    new InflationRate(2009,  2, 1.015m),
                    new InflationRate(2009,  3, 1.014m),
                    new InflationRate(2009,  4, 1.009m),
                    new InflationRate(2009,  5, 1.005m),
                    new InflationRate(2009,  6, 1.011m),
                    new InflationRate(2009,  7, 0.999m),
                    new InflationRate(2009,  8, 0.998m),
                    new InflationRate(2009,  9, 1.008m),
                    new InflationRate(2009, 10, 1.009m),
                    new InflationRate(2009, 11, 1.011m),
                    new InflationRate(2009, 12, 1.009m),
                    
                    // 2010
                    new InflationRate(2010,  1, 1.018m),
                    new InflationRate(2010,  2, 1.019m),
                    new InflationRate(2010,  3, 1.009m),
                    new InflationRate(2010,  4, 0.997m),
                    new InflationRate(2010,  5, 0.994m),
                    new InflationRate(2010,  6, 0.996m),
                    new InflationRate(2010,  7, 0.998m),
                    new InflationRate(2010,  8, 1.012m),
                    new InflationRate(2010,  9, 1.029m),
                    new InflationRate(2010, 10, 1.005m),
                    new InflationRate(2010, 11, 1.003m),
                    new InflationRate(2010, 12, 1.008m),
                    
                    // 2011
                    new InflationRate(2011,  1, 1.010m),
                    new InflationRate(2011,  2, 1.009m),
                    new InflationRate(2011,  3, 1.014m),
                    new InflationRate(2011,  4, 1.013m),
                    new InflationRate(2011,  5, 1.008m),
                    new InflationRate(2011,  6, 1.004m),
                    new InflationRate(2011,  7, 0.987m),
                    new InflationRate(2011,  8, 0.996m),
                    new InflationRate(2011,  9, 1.001m),
                    new InflationRate(2011, 10, 1.000m),
                    new InflationRate(2011, 11, 1.001m),
                    new InflationRate(2011, 12, 1.002m),
                    
                    // 2012
                    new InflationRate(2012,  1, 1.002m),
                    new InflationRate(2012,  2, 1.002m),
                    new InflationRate(2012,  3, 1.003m),
                    new InflationRate(2012,  4, 1.000m),
                    new InflationRate(2012,  5, 0.997m),
                    new InflationRate(2012,  6, 0.997m),
                    new InflationRate(2012,  7, 0.998m),
                    new InflationRate(2012,  8, 0.997m),
                    new InflationRate(2012,  9, 1.001m),
                    new InflationRate(2012, 10, 1.000m),
                    new InflationRate(2012, 11, 0.999m),
                    new InflationRate(2012, 12, 1.002m),
                    
                    // 2013
                    new InflationRate(2013,  1, 1.002m),
                    new InflationRate(2013,  2, 0.999m),
                    new InflationRate(2013,  3, 1.000m),
                    new InflationRate(2013,  4, 1.000m),
                    new InflationRate(2013,  5, 1.001m),
                    new InflationRate(2013,  6, 1.000m),
                    new InflationRate(2013,  7, 0.999m),
                    new InflationRate(2013,  8, 0.993m),
                    new InflationRate(2013,  9, 1.000m),
                    new InflationRate(2013, 10, 1.004m),
                    new InflationRate(2013, 11, 1.002m),
                    new InflationRate(2013, 12, 1.005m),
                    
                    // 2014
                    new InflationRate(2014,  1, 1.002m),
                    new InflationRate(2014,  2, 1.006m),
                    new InflationRate(2014,  3, 1.022m),
                    new InflationRate(2014,  4, 1.033m),
                    new InflationRate(2014,  5, 1.038m),
                    new InflationRate(2014,  6, 1.010m),
                    new InflationRate(2014,  7, 1.004m),
                    new InflationRate(2014,  8, 1.008m),
                    new InflationRate(2014,  9, 1.029m),
                    new InflationRate(2014, 10, 1.024m),
                    new InflationRate(2014, 11, 1.019m),
                    new InflationRate(2014, 12, 1.030m),
                    
                    // 2015
                    new InflationRate(2015,  1, 1.031m),
                    new InflationRate(2015,  2, 1.053m),
                    new InflationRate(2015,  3, 1.108m),
                    new InflationRate(2015,  4, 1.140m),
                    new InflationRate(2015,  5, 1.022m),
                    new InflationRate(2015,  6, 1.004m),
                    new InflationRate(2015,  7, 0.990m),
                    new InflationRate(2015,  8, 0.992m),
                    new InflationRate(2015,  9, 1.023m),
                    new InflationRate(2015, 10, 0.987m),
                    new InflationRate(2015, 11, 1.020m),
                    new InflationRate(2015, 12, 1.007m),
                    
                    // 2016
                    new InflationRate(2016,  1, 1.009m),
                    new InflationRate(2016,  2, 0.996m),
                    new InflationRate(2016,  3, 1.010m),
                    new InflationRate(2016,  4, 1.035m),
                    new InflationRate(2016,  5, 1.001m),
                    new InflationRate(2016,  6, 0.998m),
                    new InflationRate(2016,  7, 0.999m),
                    new InflationRate(2016,  8, 0.997m),
                    new InflationRate(2016,  9, 1.018m),
                    new InflationRate(2016, 10, 1.028m),
                    new InflationRate(2016, 11, 1.018m),
                    new InflationRate(2016, 12, 1.009m),
                    
                    // 2017
                    new InflationRate(2017,  1, 1.011m),
                    new InflationRate(2017,  2, 1.010m),
                    new InflationRate(2017,  3, 1.018m),
                    new InflationRate(2017,  4, 1.009m),
                    new InflationRate(2017,  5, 1.013m),
                    new InflationRate(2017,  6, 1.016m),
                    new InflationRate(2017,  7, 1.002m),
                    new InflationRate(2017,  8, 0.999m),
                    new InflationRate(2017,  9, 1.020m),
                    new InflationRate(2017, 10, 1.012m),
                    new InflationRate(2017, 11, 1.009m),
                    new InflationRate(2017, 12, 1.010m),
                    
                    // 2018
                    new InflationRate(2018,  1, 1.015m),
                    new InflationRate(2018,  2, 1.009m),
                    new InflationRate(2018,  3, 1.011m),
                    new InflationRate(2018,  4, 1.008m),
                    new InflationRate(2018,  5, 1.000m),
                    new InflationRate(2018,  6, 1.000m),
                    new InflationRate(2018,  7, 0.993m),
                    new InflationRate(2018,  8, 1.000m),
                    new InflationRate(2018,  9, 1.019m),
                    new InflationRate(2018, 10, 1.017m),
                    new InflationRate(2018, 11, 1.014m),
                    new InflationRate(2018, 12, 1.008m),
                    
                    // 2019
                    new InflationRate(2019,  1, 1.010m),
                    new InflationRate(2019,  2, 1.005m),
                    new InflationRate(2019,  3, 1.009m),
                    new InflationRate(2019,  4, 1.010m),
                    new InflationRate(2019,  5, 1.007m),
                    new InflationRate(2019,  6, 0.995m),
                    new InflationRate(2019,  7, 0.994m),
                    new InflationRate(2019,  8, 0.997m),
                    new InflationRate(2019,  9, 1.007m),
                    new InflationRate(2019, 10, 1.007m),
                    new InflationRate(2019, 11, 1.001m),
                    new InflationRate(2019, 12, 0.998m),
                    
                    // 2020
                    new InflationRate(2020,  1, 1.002m),
                    new InflationRate(2020,  2, 0.997m),
                    new InflationRate(2020,  3, 1.008m),
                    new InflationRate(2020,  4, 1.008m),
                    new InflationRate(2020,  5, 1.003m),
                    new InflationRate(2020,  6, 1.002m),
                    new InflationRate(2020,  7, 0.994m),
                    new InflationRate(2020,  8, 0.998m),
                    new InflationRate(2020,  9, 1.005m),
                    new InflationRate(2020, 10, 1.010m),
                    new InflationRate(2020, 11, 1.013m),
                    new InflationRate(2020, 12, 1.009m),
                    
                    // 2021
                    new InflationRate(2021,  1, 1.013m),
                    new InflationRate(2021,  2, 1.010m),
                    new InflationRate(2021,  3, 1.017m),
                    new InflationRate(2021,  4, 1.007m),
                    new InflationRate(2021,  5, 1.013m),
                    new InflationRate(2021,  6, 1.002m),
                    new InflationRate(2021,  7, 1.001m),
                    new InflationRate(2021,  8, 0.998m),
                    new InflationRate(2021,  9, 1.012m),
                    new InflationRate(2021, 10, 1.009m),
                    new InflationRate(2021, 11, 1.008m),
                    new InflationRate(2021, 12, 1.006m),
                    
                    // 2022
                    new InflationRate(2022,  1, 1.013m),
                    new InflationRate(2022,  2, 1.016m),
                    new InflationRate(2022,  3, 1.045m),
                    new InflationRate(2022,  4, 1.031m),
                    new InflationRate(2022,  5, 1.027m),
                    new InflationRate(2022,  6, 1.031m),
                    new InflationRate(2022,  7, 1.007m),
                    new InflationRate(2022,  8, 1.011m),
                    new InflationRate(2022,  9, 1.019m),
                    new InflationRate(2022, 10, 1.025m),
                    new InflationRate(2022, 11, 1.007m),
                    new InflationRate(2022, 12, 1.007m),
                    
                    // 2023
                    new InflationRate(2023,  1, 1.008m),
                    new InflationRate(2023,  2, 1.007m),
                    new InflationRate(2023,  3, 1.015m),
                    new InflationRate(2023,  4, 1.002m),
                    new InflationRate(2023,  5, 1.005m),
                    new InflationRate(2023,  6, 1.008m),
                    new InflationRate(2023,  7, 0.994m),
                    new InflationRate(2023,  8, 0.986m),
                    new InflationRate(2023,  9, 1.005m),
                    new InflationRate(2023, 10, 1.008m),
                    new InflationRate(2023, 11, 1.005m),
                    new InflationRate(2023, 12, 1.007m),
                    
                    // 2024
                    new InflationRate(2024,  1, 1.004m),
                    new InflationRate(2024,  2, 1.003m),
                    new InflationRate(2024,  3, 1.005m),
                    new InflationRate(2024,  4, 1.002m),
                    new InflationRate(2024,  5, 1.006m),
                    new InflationRate(2024,  6, 1.022m),
                    new InflationRate(2024,  7, 1.000m),
                    new InflationRate(2024,  8, 1.006m),
                    new InflationRate(2024,  9, 1.015m),
                    new InflationRate(2024, 10, 1.018m),
                    new InflationRate(2024, 11, 1.019m),
                    new InflationRate(2024, 12, 1.014m),
                    
                    // 2025
                    new InflationRate(2025,  1, 1.012m),
                    new InflationRate(2025,  2, 1.008m),
                    new InflationRate(2025,  3, 1.015m),
                    new InflationRate(2025,  4, 1.007m),
                    new InflationRate(2025,  5, 1.013m),
                    new InflationRate(2025,  6, 1.008m),
                    new InflationRate(2025,  7, 0.998m),
                    new InflationRate(2025,  8, 0.998m),
                    new InflationRate(2025,  9, 1.003m),
                    new InflationRate(2025, 10, 1.009m),
                    new InflationRate(2025, 11, 1.004m),
                    new InflationRate(2025, 12, 1.002m),
                    
                    // 2026
                    new InflationRate(2026,  1, 1.007m),
                    new InflationRate(2026,  2, 1.010m),
                    new InflationRate(2026,  3, 1.017m),
                    new InflationRate(2026,  4, 1.014m),
                    new InflationRate(2026,  5, 1.009m),
                    new InflationRate(2026,  6, 0.999m)
                };
                context.InflationRates.AddRange(inflationData);
            }


            if (!context.ExchangeRates.Any()) {
                var exchangeData = new[] {
                    // Just for testing purposes.
                    // TODO replace this with real data.
                    new ExchangeRate("USD", 2021, 1, 28.19m),
                    new ExchangeRate("USD", 2021, 2, 27.88m),
                    new ExchangeRate("USD", 2021, 3, 27.97m),
                };
                context.ExchangeRates.AddRange(exchangeData);
            }

            context.SaveChanges();
        }
    }
}
