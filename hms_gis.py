#Usage: python hms_gis.py pathToShapefile pathToNLDASGridFile

import sys
from osgeo import ogr, osr
#import geopandas
import pandas as pd
import matplotlib.pyplot as plt
import shapefile
import json
import time

def main(shapefile, gridfile):
	table = []
	colNames = []
	start = time.time()
	shape = ogr.Open(shapefile)
	nldas = ogr.Open(gridfile)

	shapeLayer = shape.GetLayer()
	nldasLayer = nldas.GetLayer()

	shapeProj = shapeLayer.GetSpatialRef()
	nldasProj = nldasLayer.GetSpatialRef()
	coordTrans = osr.CoordinateTransformation(shapeProj, nldasProj)

	colLayer = shape.GetLayer(0).GetLayerDefn()
	for i in range(colLayer.GetFieldCount()):
		colNames.append(colLayer.GetFieldDefn(i).GetName())

	coms, huc8s, huc12s = [], [], []
	overlap, polygons = [], []
	if('COMID' in colNames):
		huc12s = [None] * len(shapeLayer)	#No huc 12s
		for feature in shapeLayer:
			polygons.append(feature)
			coms.append(feature.GetField('COMID'))
			huc8s.append(feature.GetField('HUC8'))
	elif('HUC_8' in colNames):
		coms = [None] * len(shapeLayer)		#No catchments
		for feature in shapeLayer:
			polygons.append(feature)
			huc8s.append(feature.GetField('HUC_8'))
			huc12s.append(feature.GetField('HUC_12'))

	#Reproject geometries from shapefile
	for polygon in polygons:
		poly = polygon.GetGeometryRef()
		poly.Transform(coordTrans)

	#Calculate cells that contain polygons ahead of time to make intersections faster
	for feature in nldasLayer:
		cell = feature.GetGeometryRef()
		for polygon in polygons:
			poly = polygon.GetGeometryRef()
			if(poly.Intersects(cell) and feature not in overlap):
				overlap.append(feature)

	#Iterate through smaller list of overlapping cells to calculate data
	huc8table = [{"HUC 8 ID" : huc8s[0]}]
	i = 0;
	for polygon in polygons:
		poly = polygon.GetGeometryRef()
		huc12table = []
		huc12table.append({"HUC 12 ID" : huc12s[i]})
		huc12table.append({"Catchment ID": coms[i]})
		for feature in overlap:
			cell = feature.GetGeometryRef()
			interArea = 0
			squareArea = cell.Area()
			if(poly.Intersects(cell)):
				inter = poly.Intersection(cell)
				interArea += inter.Area()
				percentArea = (interArea / squareArea) * 100
				catchtable = {"latitude" : cell.Centroid().GetX(),
							  "longitude" : cell.Centroid().GetY(),
							  "cellArea" : squareArea,
							  "containedArea" : interArea,
							  "percentArea" : percentArea}
				huc12table.append(catchtable)
		huc8table.append(huc12table)
		i += 1
	table.append(huc8table)
	jsonOut = json.dumps(table, indent=4)
	#write json to file?
	print(jsonOut)
	end = time.time()
	#print(end-start)

if __name__ == "__main__":
	main(sys.argv[1], sys.argv[2])