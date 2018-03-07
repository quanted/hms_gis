#Usage: python hms_gis.py pathToShapefile pathToNLDASGridFile

import sys
import geopandas
import pandas as pd
import matplotlib.pyplot as plt
import shapefile
import json
import time

def main(shapefile, gridfile):
	table = []
	start = time.time()
	shape = geopandas.read_file(shapefile)
	nldas = geopandas.read_file(gridfile)
	shape = shape.to_crs({'init' :'epsg:4326'})
	print("Shapefile coordinate: ", shape.crs)
	print("nldas coordinate: ", nldas.crs)
	colnames = shape.columns
	coms, huc8s, huc12s = [], [], []
	
	if('COMID' in colnames):
		#No huc 12s
		coms = shape['COMID'].astype(object)
		huc8s = shape['HUC8'].astype(object)
		huc12s = [None] * len(shape)
	elif('HUC_8' in colnames):
		#No catchments
		coms = [None] * len(shape)
		huc8s = shape['HUC_8'].astype(object)
		huc12s = shape['HUC_12'].astype(object)

	#Calculate cells that contain polygons ahead of time to make intersections faster
	overlap = []
	for cell in nldas.geometry:
		for polygon in shape.geometry:
			if(polygon.intersects(cell) and cell not in overlap):
				overlap.append(cell)

	#Iterate through smaller list of overlapping cells to calculate data
	huc8table = [{"HUC 8 ID" : huc8s[0]}]
	i = 0;
	for polygon in shape.geometry:
		huc12table = []
		huc12table.append({"HUC 12 ID" : huc12s[i]})
		huc12table.append({"Catchment ID": coms[i]})
		for cell in overlap:
			interArea = 0
			squareArea = cell.area
			if(polygon.intersects(cell)):
				inter = polygon.intersection(cell)
				interArea += inter.area
				percentArea = (interArea / squareArea) * 100
				catchtable = {"latitude" : cell.centroid.x,
							  "longitude" : cell.centroid.y,
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
	print(end-start)
	#plotShapes(shape, nldas)

def plotShapes(shape, nldas):
	#Plotting figures for testing purposes
	fig, ax = plt.subplots()
	ax.set_aspect('equal')
	shape.plot(ax=ax, color='green', edgecolor='black')
	nldas.plot(ax=ax, edgecolor='black', alpha=0.4)
	plt.show()
	
if __name__ == "__main__":
	main(sys.argv[1], sys.argv[2])