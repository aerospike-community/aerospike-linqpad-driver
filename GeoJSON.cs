using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GeoJSON.Net;
using GeoJSON.Net.Converters;
using GeoJSON.Net.Geometry;
using Aerospike.Client;
using System.CodeDom;
using Newtonsoft.Json;
using GeoJSON.Net.Feature;

namespace Aerospike.Database.LINQPadDriver.Extensions
{
    public interface IGeoJSON : IGeometryObject, IGeoJSONObject
    {
    }

    public interface IGeoJSONCollection : IGeoJSON
    {
    }

    public interface IGeoJSONPosition : IPosition
    {
    }

    /// <summary>
    /// Defines the GeometryCollection type.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.8
    /// </remarks>
    public class GeoJSONCollection : GeometryCollection, IGeoJSONCollection
    {
        public GeoJSONCollection() : base(Array.Empty<IGeometryObject>())
        {}

        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryCollection" /> class.
        /// </summary>
        /// <param name="geometries">The geometries contained in this GeometryCollection.</param>
        public GeoJSONCollection(IEnumerable<IGeometryObject> geometries)
            : base(geometries) { }
        
        public GeoJSONCollection(GeometryCollection geometries)
            : base(geometries.Geometries) { }

        public static bool operator ==(GeoJSONCollection left, GeoJSONCollection right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONCollection left, GeoJSONCollection right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Defines the LineString type.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.4
    /// </remarks>
    public class GeoJSONLineString : LineString, IGeoJSON
    {
        public GeoJSONLineString(IEnumerable<IEnumerable<double>> coordinates)
        : base(coordinates)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="LineString" /> class.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        public GeoJSONLineString(IEnumerable<IPosition> coordinates)
            : base(coordinates)
        { }

        public GeoJSONLineString(LineString lineString)
            : base(lineString.Coordinates) { }

        public static bool operator ==(GeoJSONLineString left, GeoJSONLineString right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONLineString left, GeoJSONLineString right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Defines the MultiLineString type.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.5
    /// </remarks>
    public class GeoJSONMultiLineString : MultiLineString, IGeoJSON
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiLineString" /> class.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        public GeoJSONMultiLineString(IEnumerable<LineString> coordinates)
            : base (coordinates)
        { }

        /// <summary>
        /// Initializes a new <see cref="MultiLineString" /> from a 3-d array
        /// of <see cref="double" />s that matches the "coordinates" field in the JSON representation.
        /// </summary>        
        public GeoJSONMultiLineString(IEnumerable<IEnumerable<IEnumerable<double>>> coordinates)
            : base(coordinates)
        { }

        public GeoJSONMultiLineString(MultiLineString multiLineString)
            : base(multiLineString.Coordinates) { }

        public static bool operator ==(GeoJSONMultiLineString left, GeoJSONMultiLineString right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONMultiLineString left, GeoJSONMultiLineString right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Contains an array of <see cref="Point" />.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.3
    /// </remarks>
    public class GeoJSONMultiPoint : MultiPoint, IGeoJSON
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiPoint" /> class.
        /// </summary>
        /// <param name="coordinates">The coordinates.</param>
        public GeoJSONMultiPoint(IEnumerable<Point> coordinates)
            : base(coordinates)
        { }

        public GeoJSONMultiPoint(IEnumerable<GeoJSONPoint> coordinates)
            : base(coordinates.Cast<Point>())
        { }

        public GeoJSONMultiPoint(IEnumerable<IEnumerable<double>> coordinates)
        : base (coordinates)
        { }

        public GeoJSONMultiPoint(MultiPoint multiPoint)
            : base(multiPoint.Coordinates) { }

        public static bool operator ==(GeoJSONMultiPoint left, GeoJSONMultiPoint right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONMultiPoint left, GeoJSONMultiPoint right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Defines the MultiPolygon type.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.7
    /// </remarks>
    public class GeoJSONMultiPolygon : MultiPolygon, IGeoJSON
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiPolygon" /> class.
        /// </summary>
        /// <param name="polygons">The polygons contained in this MultiPolygon.</param>
        public GeoJSONMultiPolygon(IEnumerable<Polygon> polygons)
            : base(polygons)
        { }

        /// <summary>
        /// Initializes a new <see cref="MultiPolygon" /> from a 4-d array of <see cref="double" />s
        /// that matches the "coordinates" field in the JSON representation.
        /// </summary>
        public GeoJSONMultiPolygon(IEnumerable<IEnumerable<IEnumerable<IEnumerable<double>>>> coordinates)
            : base(coordinates)
        { }

        public GeoJSONMultiPolygon(MultiPolygon multiPolygon)
            : base(multiPolygon.Coordinates) { }

        public static bool operator ==(GeoJSONMultiPolygon left, GeoJSONMultiPolygon right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONMultiPolygon left, GeoJSONMultiPolygon right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Defines the Point type.
    /// In geography, a point refers to a Position on a map, expressed in latitude and longitude.
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.2
    /// </remarks>
    public class GeoJSONPoint : Point, IGeoJSON
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Point" /> class.
        /// </summary>
        /// <param name="coordinates">The Position.</param>
        public GeoJSONPoint(IPosition coordinates)
            : base(coordinates)
        { }

        public GeoJSONPoint(Point point)
            : base(point.Coordinates) { }

        public static bool operator ==(GeoJSONPoint left, GeoJSONPoint right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONPoint left, GeoJSONPoint right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Defines the Polygon type.
    /// Coordinates of a Polygon are a list of linear rings coordinate arrays. The first element in 
    /// the array represents the exterior ring. Any subsequent elements represent interior rings (or holes).
    /// </summary>
    /// <remarks>
    /// See https://tools.ietf.org/html/rfc7946#section-3.1.6
    /// </remarks>
    public class GeoJSONPolygon : Polygon, IGeoJSON
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Polygon" /> class.
        /// </summary>
        /// <param name="coordinates">
        /// The linear rings with the first element in the array representing the exterior ring. 
        /// Any subsequent elements represent interior rings (or holes).
        /// </param>
        public GeoJSONPolygon(IEnumerable<LineString> coordinates)
            : base(coordinates)
        { }

        /// <summary>
        /// Initializes a new <see cref="Polygon" /> from a 3-d array of <see cref="double" />s
        /// that matches the "coordinates" field in the JSON representation.
        /// </summary>       
        public GeoJSONPolygon(IEnumerable<IEnumerable<IEnumerable<double>>> coordinates)
            : base(coordinates)
        { }

        public GeoJSONPolygon(Polygon polygon)
            : base(polygon.Coordinates) { }

        public static bool operator ==(GeoJSONPolygon left, GeoJSONPolygon right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONPolygon left, GeoJSONPolygon right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// A position is the fundamental geometry construct, consisting of Latitude,
    /// Longitude and (optionally) Altitude.
    /// </summary>
    public class GeoJSONPosition : Position, IGeoJSONPosition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Position" /> class.
        /// </summary>
        /// <param name="latitude">The latitude, or Y coordinate.</param>
        /// <param name="longitude">The longitude or X coordinate.</param>
        /// <param name="altitude">The altitude in m(eter).</param>
        public GeoJSONPosition(double latitude, double longitude, double? altitude = null)
            : base(latitude, longitude, altitude)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Position" /> class.
        /// </summary>
        /// <param name="latitude">The latitude, or Y coordinate e.g. '38.889722'.</param>
        /// <param name="longitude">The longitude, or X coordinate e.g. '-77.008889'.</param>
        /// <param name="altitude">The altitude in m(eters).</param>
        public GeoJSONPosition(string latitude, string longitude, string altitude = null)
            : base(latitude, longitude, altitude)
        { }
        
        public GeoJSONPosition(IPosition position)
            : base(position.Latitude, position.Longitude, position.Altitude) { }

        public static bool operator ==(GeoJSONPosition left, GeoJSONPosition right)
        {
            if ((object)left == right)
            {
                return true;
            }

            if ((object)right is null)
            {
                return false;
            }

            if (left is not null)
            {
                return left.Equals(right);
            }

            return false;
        }

        public static bool operator !=(GeoJSONPosition left, GeoJSONPosition right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    public static class GeoJSONHelpers
    {
        public static object ConvertToGeoJson(this Value.GeoJSONValue geoValue)
            => ConvertToGeoJson(geoValue?.value);

        public static object ConvertToGeoJson(string geoValue)
        {
            //System.Diagnostics.Debugger.Launch();

            if (string.IsNullOrEmpty(geoValue)) return null;

            var geoObj = JsonConvert.DeserializeObject<GeoJSONObject>(geoValue, new GeoJsonConverter());

            switch (geoObj.Type)
            {
                case GeoJSONObjectType.Polygon:
                    return new GeoJSONPolygon((Polygon)geoObj);
                case GeoJSONObjectType.MultiPolygon:
                    return new GeoJSONMultiPolygon((MultiPolygon)geoObj);
                case GeoJSONObjectType.MultiPoint:
                    return new GeoJSONMultiPoint((MultiPoint)geoObj);
                case GeoJSONObjectType.MultiLineString:
                    return new GeoJSONMultiLineString((MultiLineString)geoObj);
                case GeoJSONObjectType.Point:
                    return new GeoJSONPoint((Point)geoObj);
                case GeoJSONObjectType.LineString:
                    return new GeoJSONLineString((LineString)geoObj);
                case GeoJSONObjectType.GeometryCollection:
                    return new GeoJSONCollection((GeometryCollection)geoObj);
                default:
                    if (geoObj is IPosition pos)
                        return new GeoJSONPosition(pos);
                    break;
            }

            return geoObj;
        }

        public static Value.GeoJSONValue ConvertFromGeoJson(IGeoJSONObject geoValue)
        {
            if (geoValue is null) return null;

            return new Value.GeoJSONValue(JsonConvert.SerializeObject(geoValue));
        }

        public static Value.GeoJSONValue ConvertFromGeoJson(object geoValue)
        {
            if (geoValue is null) return null;
            
            return new Value.GeoJSONValue(JsonConvert.SerializeObject(geoValue));            
        }

        public static bool IsGeoValue(Type checkType)
            => Helpers.IsSubclassOfInterface(typeof(IGeoJSONObject), checkType); 
    }
}
