#Usage: python hms_gis.py pathToShapefile pathToNLDASGridFile


import sys
import geopandas
import pandas as pd
import matplotlib.pyplot as plt
import shapefile


def main(shapefile, gridfile):
	columnNames = ['Latitude', 'Longitude', 'Cell Area', 'Contained Area', 'Percent Area']
	table = pd.DataFrame(columns = columnNames)

	shape = geopandas.read_file(shapefile)
	nldas = geopandas.read_file(gridfile)

	#Calculate cells that contain polygons ahead of time to make intersections faster
	overlap = []
	for cell in nldas.geometry:
		for polygon in shape.geometry:
			if(polygon.intersects(cell) and cell not in overlap):
				overlap.append(cell)

	#iterate through smaller list of overlapping cells to calculate data
	for cell in overlap:
		interArea = 0
		squareArea = cell.area
		for polygon in shape.geometry:
			if(polygon.intersects(cell)):
				inter = polygon.intersection(cell)
				interArea += inter.area
		percentArea = (interArea / squareArea) * 100
		dataList = [cell.centroid.x,cell.centroid.y,squareArea,interArea,percentArea]
		row = pd.DataFrame([dataList], columns = columnNames)
		table = table.append(row)
	print(table)
	
	
if __name__ == "__main__":
	main(sys.argv[1], sys.argv[2])