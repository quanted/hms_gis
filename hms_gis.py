#Usage: python hms_gis.py pathToShapefile pathToNLDASGridFile

import sys
from osgeo import ogr, osr
#import geopandas
import pandas as pd
import matplotlib.pyplot as plt
import urllib.request
from zipfile import *
import shutil
import json
import time
import os

def main(huc_8_num, com_id_num):
	start = time.time()
	table = []
	colNames = []
	url = 'ftp://newftp.epa.gov/exposure/BasinsData/NHDPlus21/NHDPlus' + str(huc_8_num) + '.zip'
	nldasurl = 'https://ldas.gsfc.nasa.gov/nldas/gis/NLDAS_Grid_Reference.zip'
	shapefile = urllib.request.urlretrieve(url, 'shape.zip')
	gridfile = urllib.request.urlretrieve(nldasurl, 'grid.zip')

	with ZipFile('shape.zip') as myzip:
		myzip.extractall()
	with ZipFile('grid.zip') as myzip:
		myzip.extractall('NLDAS')

	sfile = 'NHDPlus' + str(huc_8_num) + '/Drainage/Catchment.shp'
	gfile = 'NLDAS/NLDAS_Grid_Reference.shp'

	shape = ogr.Open(sfile)
	nldas = ogr.Open(gfile)

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
			if(feature.GetField('COMID') == int(com_id_num)): # Only focusing on the given catchment argument
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
	print(end - start)
	shape = None
	nldas = None
	#Delete zipfiles and extracted shapefiles
	os.remove('grid.zip')
	os.remove('shape.zip')
	shutil.rmtree('NHDPlus' + str(huc_8_num))
	shutil.rmtree('NLDAS')


if __name__ == "__main__":
	main(sys.argv[1], sys.argv[2])