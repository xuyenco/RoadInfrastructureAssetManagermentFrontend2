using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using RoadInfrastructureAssetManagementFrontend2.Model.Geometry;
using System.Collections.Generic;
using System.Text.Json;

public static class CoordinateConverter
{
    private static readonly CoordinateSystemFactory csFactory = new CoordinateSystemFactory();
    private static readonly CoordinateTransformationFactory ctFactory = new CoordinateTransformationFactory();

    private static readonly CoordinateSystem vn2000 = csFactory.CreateFromWkt(
        "PROJCS[\"VN-2000 / UTM zone 48N\",GEOGCS[\"VN-2000\",DATUM[\"Vietnam_2000\",SPHEROID[\"WGS 84\",6378137,298.257223563],TOWGS84[-191.90441429,-39.30318279,-111.45032835,-0.00928836,0.01975479,-0.00427372,0.252906278]],PRIMEM[\"Greenwich\",0,AUTHORITY[\"EPSG\",\"8901\"]],UNIT[\"degree\",0.0174532925199433,AUTHORITY[\"EPSG\",\"9122\"]],AUTHORITY[\"EPSG\",\"4756\"]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",105],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"metre\",1,AUTHORITY[\"EPSG\",\"9001\"]],AXIS[\"Easting\",EAST],AXIS[\"Northing\",NORTH],AUTHORITY[\"EPSG\",\"3405\"]]");

    private static readonly CoordinateSystem wgs84 = GeographicCoordinateSystem.WGS84;

    private static readonly ICoordinateTransformation transform = ctFactory.CreateFromCoordinateSystems(vn2000, wgs84);

    public static GeoJsonGeometry ConvertGeometryToWGS84(GeoJsonGeometry geometry, int utmZone = 48)
    {
        if (geometry == null || string.IsNullOrEmpty(geometry.type) || geometry.coordinates == null)
        {
            Console.WriteLine("Invalid geometry: null or missing type/coordinates");
            return geometry;
        }

        var result = new GeoJsonGeometry
        {
            type = geometry.type,
            coordinates = ProcessCoordinates(geometry.coordinates)
        };
        return result;
    }

    private static object ProcessCoordinates(object coordinates)
    {
        if (coordinates is JsonElement jsonElement)
        {
            return ProcessJsonElement(jsonElement);
        }
        return coordinates;
    }

    private static object ProcessJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                var firstItem = element[0];
                if (firstItem.ValueKind == JsonValueKind.Number && element.GetArrayLength() == 2)
                {
                    double x = element[0].GetDouble();
                    double y = element[1].GetDouble();
                    var wgs84Coords = transform.MathTransform.Transform(new double[] { x, y });
                    return new double[] { wgs84Coords[0], wgs84Coords[1] };
                }
                else if (firstItem.ValueKind == JsonValueKind.Array && firstItem.GetArrayLength() == 2)
                {
                    var lineCoords = new double[element.GetArrayLength()][];
                    for (int i = 0; i < element.GetArrayLength(); i++)
                    {
                        double x = element[i][0].GetDouble();
                        double y = element[i][1].GetDouble();
                        var wgs84Coords = transform.MathTransform.Transform(new double[] { x, y });
                        lineCoords[i] = new double[] { wgs84Coords[0], wgs84Coords[1] };
                    }
                    return lineCoords;
                }
                else if (firstItem.ValueKind == JsonValueKind.Array && firstItem[0].ValueKind == JsonValueKind.Array && firstItem[0].GetArrayLength() == 2)
                {
                    var polyCoords = new double[element.GetArrayLength()][][];
                    for (int i = 0; i < element.GetArrayLength(); i++)
                    {
                        var ring = element[i];
                        polyCoords[i] = new double[ring.GetArrayLength()][];
                        for (int j = 0; j < ring.GetArrayLength(); j++)
                        {
                            double x = ring[j][0].GetDouble();
                            double y = ring[j][1].GetDouble();
                            var wgs84Coords = transform.MathTransform.Transform(new double[] { x, y });
                            polyCoords[i][j] = new double[] { wgs84Coords[0], wgs84Coords[1] };
                        }
                    }
                    return polyCoords;
                }
                break;
        }
        return element;
    }

    private static readonly ICoordinateTransformation inverseTransform = ctFactory.CreateFromCoordinateSystems(wgs84, vn2000);

    public static GeoJsonGeometry ConvertGeometryToVN2000(GeoJsonGeometry geometry, int utmZone = 48)
    {
        if (geometry == null || string.IsNullOrEmpty(geometry.type) || geometry.coordinates == null)
        {
            Console.WriteLine("Invalid geometry: null or missing type/coordinates");
            return geometry;
        }

        var result = new GeoJsonGeometry
        {
            type = geometry.type,
            coordinates = ProcessInverseCoordinates(geometry.coordinates)
        };
        return result;
    }

    private static object ProcessInverseCoordinates(object coordinates)
    {
        if (coordinates is JsonElement jsonElement)
        {
            return ProcessInverseJsonElement(jsonElement);
        }
        return coordinates;
    }

    private static object ProcessInverseJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                var firstItem = element[0];
                if (firstItem.ValueKind == JsonValueKind.Number && element.GetArrayLength() == 2)
                {
                    double lon = element[0].GetDouble();
                    double lat = element[1].GetDouble();
                    var vn2000Coords = inverseTransform.MathTransform.Transform(new double[] { lon, lat });
                    return new double[] { vn2000Coords[0], vn2000Coords[1] };
                }
                else if (firstItem.ValueKind == JsonValueKind.Array && firstItem.GetArrayLength() == 2)
                {
                    var lineCoords = new double[element.GetArrayLength()][];
                    for (int i = 0; i < element.GetArrayLength(); i++)
                    {
                        double lon = element[i][0].GetDouble();
                        double lat = element[i][1].GetDouble();
                        var vn2000Coords = inverseTransform.MathTransform.Transform(new double[] { lon, lat });
                        lineCoords[i] = new double[] { vn2000Coords[0], vn2000Coords[1] };
                    }
                    return lineCoords;
                }
                else if (firstItem.ValueKind == JsonValueKind.Array && firstItem[0].ValueKind == JsonValueKind.Array && firstItem[0].GetArrayLength() == 2)
                {
                    var polyCoords = new double[element.GetArrayLength()][][];
                    for (int i = 0; i < element.GetArrayLength(); i++)
                    {
                        var ring = element[i];
                        polyCoords[i] = new double[ring.GetArrayLength()][];
                        for (int j = 0; j < ring.GetArrayLength(); j++)
                        {
                            double lon = ring[j][0].GetDouble();
                            double lat = ring[j][1].GetDouble();
                            var vn2000Coords = inverseTransform.MathTransform.Transform(new double[] { lon, lat });
                            polyCoords[i][j] = new double[] { vn2000Coords[0], vn2000Coords[1] };
                        }
                    }
                    return polyCoords;
                }
                break;
        }
        return element;
    }

}