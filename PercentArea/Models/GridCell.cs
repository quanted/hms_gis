using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NetTopologySuite.IO;
using System.Collections;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Web.Services;
using System.Web.UI.WebControls;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using GeoAPI.CoordinateSystems;
using ProjNet.Converters.WellKnownText;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;
using GeoAPI.CoordinateSystems.Transformations;
using GeoAPI.Geometries;
using NetTopologySuite.Geometries;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Web.Script.Services;
using Newtonsoft.Json;
using NetTopologySuite.Features;

namespace PercentArea.Models
{
    public class GridCell
    {        
        public List<Object> CalculateDataTable(string polyfile)
        {
            //List<GeoAPI.Geometries.IGeometry> polys = new List<GeoAPI.Geometries.IGeometry>();
            List<GeoAPI.Geometries.IGeometry> squares = new List<GeoAPI.Geometries.IGeometry>();
            ArrayList polys = new ArrayList();
            List<Object> infoTable = new List<Object>();
            double squareArea = 0;//0.015625;
            double gridArea = 0;
            double polygonArea = 0;
            string catchmentID = "";


            //////////////
            string gridfile = @"M:\DotSpatTopology\tests\NLDAS_Grid_Reference.shp";//"";
            string gridproj = @"M:\DotSpatTopology\tests\NLDAS_Grid_Reference.prj";
            //////////////

            Guid gid = Guid.NewGuid();
            string directory = @"M:\\TransientStorage\\" + gid.ToString() + "\\";


            //This block is for getting and setting shapefiles for NLDAS Grid
            /**
            client.DownloadFile("https://ldas.gsfc.nasa.gov/nldas/gis/NLDAS_Grid_Reference.zip", @"M:\\TransientStorage\\NLDAS.zip");
            ZipFile.ExtractToDirectory(@"M:\\TransientStorage\\NLDAS.zip", @"M:\\TransientStorage\\NLDAS");
            unzippedLocation = (@"M:\\TransientStorage\\NLDAS");
            foreach (string file in Directory.GetFiles(unzippedLocation))
            {
                if (Path.GetExtension(file).Equals(".shp"))
                {
                    gridfile = file;
                }
                else if (Path.GetExtension(file).Equals(".prj"))
                {
                    gridproj = file;
                }
            }
            client.Dispose();**/


            ShapefileDataReader reader2 = new ShapefileDataReader(gridfile, NetTopologySuite.Geometries.GeometryFactory.Default);
            while (reader2.Read())
            {
                squares.Add(reader2.Geometry);
                gridArea += reader2.Geometry.Area;
            }

            reader2.Dispose();



            //if (polyfile.StartsWith(@"{""type"": ""FeatureCollection"""))
            if (polyfile.StartsWith(@"{""type"":"))//.geojson
            {
                Boolean version1 = true;
                string[] featureParams = new string[3];
                string jsonfile = polyfile;
                var readera = new NetTopologySuite.IO.GeoJsonReader();
                NetTopologySuite.Features.FeatureCollection result = readera.Read<NetTopologySuite.Features.FeatureCollection>(jsonfile);
                if (result[0].Attributes.GetNames().Contains("HUC_8"))
                {
                    version1 = false;
                    featureParams[0] = "OBJECTID";
                    featureParams[1] = "HUC_8";
                    featureParams[2] = "HUC_12";
                }
                else if(result[0].Attributes.GetNames().Contains("HUC8"))
                {
                    version1 = true;
                    featureParams[0] = "COMID";
                    featureParams[1] = "HUC8";
                    featureParams[2] = "HUC12";
                }
                else
                {
                    version1 = false;
                    featureParams[0] = null;
                    featureParams[1] = null;
                    featureParams[2] = null;
                }
                

                List<Object> huc8Table = new List<Object>();
                huc8Table.Add(new KeyValuePair<string, string>("HUC 8 ID: ", result[0].Attributes[featureParams[1]].ToString()));
                
                for (int i = 0; i < result.Count; i++)
                {
                    List<Object> huc12Table = new List<Object>();
                    if (version1)
                    {
                        huc12Table.Add(new KeyValuePair<string, string>("HUC 12 ID: ", null));
                    }
                    else
                    {
                        huc12Table.Add(new KeyValuePair<string, string>("HUC 12 ID: ", result[i].Attributes["HUC_12"].ToString()));
                    }
                    
                    catchmentID = result[i].Attributes[featureParams[0]].ToString();
                    huc12Table.Add(new KeyValuePair<string, string>("Catchment ID: ", catchmentID));
                    foreach (GeoAPI.Geometries.IGeometry s in squares)
                    {
                        double interArea = 0.0;
                        squareArea = s.Area;
                        if (result[i].Geometry.Intersects(s))
                        {
                            GeoAPI.Geometries.IGeometry intersection = result[i].Geometry.Intersection(s);
                            interArea += intersection.Area;
                            double percent2 = (interArea / squareArea) * 100;
                            Dictionary<string, string> catchTable = new Dictionary<string, string>();
                            //catchTable.Add("catchmentID", catchmentID);
                            catchTable.Add("latitude", s.Centroid.X.ToString());
                            catchTable.Add("longitude", s.Centroid.Y.ToString());
                            catchTable.Add("cellArea", squareArea.ToString());
                            catchTable.Add("containedArea", interArea.ToString());
                            catchTable.Add("percentArea", percent2.ToString());
                            huc12Table.Add(catchTable);
                        }
                        
                    }
                    huc8Table.Add(huc12Table);
                }
                infoTable.Add(huc8Table);
            }
            else                                                    //Huc ID
            {
                catchmentID = polyfile;
                string ending = polyfile + ".zip";
                
                WebClient client = new WebClient();
                DirectoryInfo di = Directory.CreateDirectory(directory);
                client.DownloadFile("ftp://newftp.epa.gov/exposure/NHDV1/HUC12_Boundries/" + ending, directory + ending);

                string projfile = "";
                string dataFile = "";

                ZipFile.ExtractToDirectory(directory + ending, directory + polyfile);
                string unzippedLocation = (directory + polyfile + "\\" + polyfile); //+ "\\NHDPlus" + polyfile + "\\Drainage");
                foreach (string file in Directory.GetFiles(unzippedLocation))
                {
                    if (Path.GetExtension(file).Equals(".shp"))
                    {
                        polyfile = file;
                    }
                    else if (Path.GetExtension(file).Equals(".prj"))
                    {
                        projfile = file;
                    }
                    else if (Path.GetExtension(file).Equals(".dbf"))
                    {
                        dataFile = file;
                    }
                }

                //This block is for setting projection parameters of input shapefile and projecting it to NLDAS grid
                //Reprojecting of coordinates is not needed for NHDPlus V2

                string line = System.IO.File.ReadAllText(projfile);
                string[] projParams = { "PARAMETER", @"PARAMETER[""latitude_Of_origin"",0]," };//@"PARAMETER[""false_easting"",0],", @"PARAMETER[""false_northing"",0],", @"PARAMETER[""central_meridian"",0],", @"PARAMETER[""standard_parallel_1"",0],", @"PARAMETER[""standard_parallel_2"",0],", @"PARAMETER[""latitude_Of_origin"",0]," };
                int ptr = 0;
                foreach (string x in projParams)
                {
                    if (line.Contains(x))
                    {
                        ptr = line.IndexOf(x);
                    }
                    else if (!line.Contains(x) && !x.Equals("PARAMETER"))
                    {
                        line = line.Insert(ptr, x);
                    }
                }
                string line2 = System.IO.File.ReadAllText(gridproj);

                IProjectedCoordinateSystem pcs = CoordinateSystemWktReader.Parse(line) as IProjectedCoordinateSystem;
                IGeographicCoordinateSystem gcs = GeographicCoordinateSystem.WGS84 as IGeographicCoordinateSystem;

                CoordinateTransformationFactory ctfac = new CoordinateTransformationFactory();
                ICoordinateTransformation transformTo = ctfac.CreateFromCoordinateSystems(pcs, gcs);
                IMathTransform inverseTransformTo = transformTo.MathTransform;


                //Read geometries from both shapefiles and store in array lists
                //As well as calculate shapefile areas ahead of time
                ShapefileDataReader reader = new ShapefileDataReader(polyfile, NetTopologySuite.Geometries.GeometryFactory.Default);
                while (reader.Read())
                {
                    //Reprojection not needed for NHDPLUSV2
                    CoordinateList cordlist = new CoordinateList();
                    foreach (Coordinate coord in reader.Geometry.Coordinates)
                    {
                        double[] newCoord = {coord.X, coord.Y};
                        newCoord = inverseTransformTo.Transform(newCoord);
                        Coordinate newpt = new Coordinate(newCoord[0], newCoord[1]);
                        cordlist.Add(newpt);
                    }
                    Coordinate[] listofpts = cordlist.ToCoordinateArray();
                    IGeometryFactory geoFactory = new NetTopologySuite.Geometries.GeometryFactory();
                    NetTopologySuite.Geometries.LinearRing linear = (NetTopologySuite.Geometries.LinearRing)new GeometryFactory().CreateLinearRing(listofpts);
                    Polygon projPoly = new Polygon(linear, null, geoFactory);
                    
                    polys.Add(projPoly);
                    polygonArea += projPoly.Area;
                }
                reader.Dispose();
                
                List<Object> huc8Table = new List<Object>();
                huc8Table.Add(new KeyValuePair<string, string>("HUC 8 ID: ", catchmentID));
                
                foreach(Polygon p in polys)
                {
                    List<Object> huc12Table = new List<Object>();
                    huc12Table.Add(new KeyValuePair<string, string>("HUC 12 ID: ", null));
                    catchmentID = null;//result[i].Attributes["OBJECTID"].ToString();
                    huc12Table.Add(new KeyValuePair<string, string>("Catchment ID: ", catchmentID));
                    foreach (GeoAPI.Geometries.IGeometry s in squares)
                    {
                        double interArea = 0.0;
                        squareArea = s.Area;
                        if (p.Intersects(s))
                        {
                            GeoAPI.Geometries.IGeometry intersection = p.Intersection(s);
                            interArea += intersection.Area;
                            double percent2 = (interArea / squareArea) * 100;
                            Dictionary<string, string> catchTable = new Dictionary<string, string>();
                            //catchTable.Add("catchmentID", catchmentID);
                            catchTable.Add("latitude", s.Centroid.X.ToString());
                            catchTable.Add("longitude", s.Centroid.Y.ToString());
                            catchTable.Add("cellArea", squareArea.ToString());
                            catchTable.Add("containedArea", interArea.ToString());
                            catchTable.Add("percentArea", percent2.ToString());
                            huc12Table.Add(catchTable);
                        }

                    }
                    huc8Table.Add(huc12Table);
                }
                infoTable.Add(huc8Table);
            }

            //System.IO.DirectoryInfo del = new DirectoryInfo(directory);
            /*
            foreach (FileInfo file in del.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in del.GetDirectories())
            {
                dir.Delete(true);
            }*/
            //del.Delete(true);
            /////
            //infoTable.Add(new List<Object>() { elapsedTime, elapsedTime, elapsedTime, elapsedTime });
            //////

            return infoTable;
        }
    }
}