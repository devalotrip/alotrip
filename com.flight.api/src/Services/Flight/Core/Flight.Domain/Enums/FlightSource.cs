namespace Flight.Domain.Enums;

/// <summary>
/// Nguồn dữ liệu chuyến bay — map từ IBE engines cũ
/// </summary>
public enum FlightSource
{
    Galileo = 0,   // GDS Travelport/Galileo
    Datacom = 1,   // LCC Domestic (VietJet, Bamboo, Jetstar)
    Kiwi    = 2,   // Kiwi.com REST API
    Pkfare  = 3,   // Pkfare REST API
    Maybay  = 4    // Maybay.vn
}
