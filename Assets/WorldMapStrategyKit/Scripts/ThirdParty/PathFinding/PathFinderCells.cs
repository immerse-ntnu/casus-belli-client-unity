//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//
//  Some modifications by Kronnect to reuse grid buffers between calls and to allow different grid configurations in same grid array (uses bitwise differentiator)
//  Also including support for hexagonal grids and some other improvements

using UnityEngine;
using System;
using System.Collections.Generic;

namespace WorldMapStrategyKit.PathFinding
{
	public class PathFinderCells : IPathFinder
	{
		// Heap variables are initializated to default, but I like to do it anyway
		private CellCosts[] mGrid = null;
		private PriorityQueueB<int> mOpen = null;
		private List<PathFinderNode> mClose = new();
		private HeuristicFormula mFormula = HeuristicFormula.Manhattan;
		private float mHEstimate = 1;
		private int mMaxSteps = 2000;
		private float mMaxSearchCost = 100000;
		private PathFinderNodeFast[] mCalcGrid = null;
		private byte mOpenNodeValue = 1;
		private byte mCloseNodeValue = 2;
		private OnCellCross mOnCellCross = null;
		private float mMinAltitude = 0;
		private float mMaxAltitude = 1f;

		//Promoted local variables to member variables to avoid recreation between calls
		private float mH = 0;
		private int mLocation = 0;
		private int mNewLocation = 0;
		private ushort mLocationX = 0;
		private ushort mLocationY = 0;
		private ushort mNewLocationX = 0;
		private ushort mNewLocationY = 0;
		private int mCloseNodeCounter = 0;
		private ushort mGridX = 0;
		private ushort mGridY = 0;
		private bool mFound = false;

		private sbyte[,] mDirectionHex0 = new sbyte[6, 2]
		{
			{ 0, -1 },
			{ 1, 0 },
			{ 0, 1 },
			{ -1, 0 },
			{ 1, 1 },
			{ -1, 1 }
		};

		private sbyte[,] mDirectionHex1 = new sbyte[6, 2]
		{
			{ 0, -1 },
			{ 1, 0 },
			{ 0, 1 },
			{ -1, 0 },
			{ -1, -1 },
			{ 1, -1 }
		};

		private int[] mCellSide0 = new int[6]
		{
			(int)CELL_SIDE.Bottom,
			(int)CELL_SIDE.BottomRight,
			(int)CELL_SIDE.Top,
			(int)CELL_SIDE.BottomLeft,
			(int)CELL_SIDE.TopRight,
			(int)CELL_SIDE.TopLeft
		};

		private int[] mCellSide1 = new int[6]
		{
			(int)CELL_SIDE.Bottom,
			(int)CELL_SIDE.TopRight,
			(int)CELL_SIDE.Top,
			(int)CELL_SIDE.TopLeft,
			(int)CELL_SIDE.BottomLeft,
			(int)CELL_SIDE.BottomRight
		};

		private int mEndLocation;
		private float mNewG;
		private TERRAIN_CAPABILITY mTerrainCapability = TERRAIN_CAPABILITY.Any;
		private int callNumber;

		public PathFinderCells(CellCosts[] grid, int gridWidth, int gridHeight)
		{
			if (grid == null)
				throw new Exception("Grid cannot be null");

			mGrid = grid;
			mGridX = (ushort)gridWidth;
			mGridY = (ushort)gridHeight;

			if (mCalcGrid == null || mCalcGrid.Length != mGridX * mGridY)
				mCalcGrid = new PathFinderNodeFast[mGridX * mGridY];

			mOpen = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));
		}

		public void SetCustomCellsCosts(CellCosts[] cellsCosts)
		{
			mGrid = cellsCosts;
		}

		public HeuristicFormula Formula { get => mFormula; set => mFormula = value; }

		public TERRAIN_CAPABILITY TerrainCapability
		{
			get => mTerrainCapability;
			set => mTerrainCapability = value;
		}

		public float HeuristicEstimate { get => mHEstimate; set => mHEstimate = value; }

		public float MaxSearchCost { get => mMaxSearchCost; set => mMaxSearchCost = value; }

		public int MaxSteps { get => mMaxSteps; set => mMaxSteps = value; }

		public OnCellCross OnCellCross { get => mOnCellCross; set => mOnCellCross = value; }

		public float MinAltitude { get => mMinAltitude; set => mMinAltitude = value; }

		public float MaxAltitude { get => mMaxAltitude; set => mMaxAltitude = value; }

		public List<PathFinderNode> FindPath(Point start, Point end, out float totalCost)
		{
			totalCost = 0;

			mFound = false;
			mCloseNodeCounter = 0;
			if (mOpenNodeValue > 250)
			{
				Array.Clear(mCalcGrid, 0, mCalcGrid.Length);
				mOpenNodeValue = 1;
				mCloseNodeValue = 2;
			}
			else
			{
				mOpenNodeValue += 2;
				mCloseNodeValue += 2;
			}
			mOpen.Clear();
			mClose.Clear();
			callNumber++;

			mLocation = start.Y * mGridX + start.X;
			mEndLocation = end.Y * mGridX + end.X;
			mCalcGrid[mLocation].G = 0;
			mCalcGrid[mLocation].F = mHEstimate;
			mCalcGrid[mLocation].PX = (ushort)start.X;
			mCalcGrid[mLocation].PY = (ushort)start.Y;
			mCalcGrid[mLocation].Status = mOpenNodeValue;

			mOpen.Push(mLocation);
			while (mOpen.Count > 0)
			{
				mLocation = mOpen.Pop();

				// Is it in closed list? means this node was already processed
				if (mCalcGrid[mLocation].Status == mCloseNodeValue)
					continue;

				if (mLocation == mEndLocation)
				{
					mCalcGrid[mLocation].Status = mCloseNodeValue;
					mFound = true;
					break;
				}

				if (mCloseNodeCounter > mMaxSteps)
					return null;

				mLocationX = (ushort)(mLocation % mGridX);
				mLocationY = (ushort)(mLocation / mGridX);

				//Lets calculate each successors
				for (var i = 0; i < 6; i++)
				{
					int cellSide;
					if (mLocationX % 2 == 0)
					{
						mNewLocationX = (ushort)(mLocationX + mDirectionHex0[i, 0]);
						mNewLocationY = (ushort)(mLocationY + mDirectionHex0[i, 1]);
						cellSide = mCellSide0[i];
					}
					else
					{
						mNewLocationX = (ushort)(mLocationX + mDirectionHex1[i, 0]);
						mNewLocationY = (ushort)(mLocationY + mDirectionHex1[i, 1]);
						cellSide = mCellSide1[i];
					}

					if (mNewLocationY >= mGridY)
						continue;

					if (mNewLocationX >= mGridX)
						continue;

					// Unbreakeable?
					mNewLocation = mNewLocationY * mGridX + mNewLocationX;
					if (mGrid[mNewLocation].isBlocked)
						continue;

					if (mGrid[mNewLocation].altitude < mMinAltitude ||
					    mGrid[mNewLocation].altitude > mMaxAltitude)
						continue;

					if (mTerrainCapability != TERRAIN_CAPABILITY.Any)
					{
						var isWater = mGrid[mNewLocation].isWater;
						if (mTerrainCapability == TERRAIN_CAPABILITY.OnlyGround)
						{
							if (isWater)
								continue;
						}
						else
						{
							if (!isWater)
								continue;
						}
					}

					float gridValue = 1;
					var sideCosts = mGrid[mLocation].crossCost;
					if (sideCosts != null)
						gridValue = sideCosts[cellSide];

					// Check custom validator
					if (mOnCellCross != null)
					{
						if (mGrid[mNewLocation].cachedCallNumber != callNumber)
						{
							mGrid[mNewLocation].cachedCallNumber = callNumber;
							mGrid[mNewLocation].cachedEventCostValue = mOnCellCross(mNewLocation);
						}
						gridValue += mGrid[mNewLocation].cachedEventCostValue;
					}
					if (gridValue <= 0)
						gridValue = 1;

					mNewG = mCalcGrid[mLocation].G + gridValue;

					if (mNewG > mMaxSearchCost)
						continue;

					//Is it open or closed?
					if (mCalcGrid[mNewLocation].Status == mOpenNodeValue ||
					    mCalcGrid[mNewLocation].Status ==
					    mCloseNodeValue) // The current node has less code than the previous? then skip this node
						if (mCalcGrid[mNewLocation].G <= mNewG)
							continue;

					mCalcGrid[mNewLocation].PX = mLocationX;
					mCalcGrid[mNewLocation].PY = mLocationY;
					mCalcGrid[mNewLocation].G = mNewG;

					var dist = Math.Abs(mNewLocationX - end.X);
					switch (mFormula)
					{
						default:
						case HeuristicFormula.Manhattan:
							mH = mHEstimate * (dist + Math.Abs(mNewLocationY - end.Y));
							break;
						case HeuristicFormula.MaxDXDY:
							mH = mHEstimate * Math.Max(dist, Math.Abs(mNewLocationY - end.Y));
							break;
						case HeuristicFormula.DiagonalShortCut:
							float h_diagonal = Math.Min(dist, Math.Abs(mNewLocationY - end.Y));
							float h_straight = dist + Math.Abs(mNewLocationY - end.Y);
							mH = mHEstimate * 2 * h_diagonal + mHEstimate * (h_straight - 2 * h_diagonal);
							break;
						case HeuristicFormula.Euclidean:
							mH = mHEstimate *
							     Mathf.Sqrt(Mathf.Pow(dist, 2) + Mathf.Pow(mNewLocationY - end.Y, 2));
							break;
						case HeuristicFormula.EuclideanNoSQR:
							mH = mHEstimate * (Mathf.Pow(dist, 2) + Mathf.Pow(mNewLocationY - end.Y, 2));
							break;
						case HeuristicFormula.Custom1:
							var dxy = new Point(dist, Math.Abs(end.Y - mNewLocationY));
							float Orthogonal = Math.Abs(dxy.X - dxy.Y);
							var Diagonal = Math.Abs((dxy.X + dxy.Y - Orthogonal) / 2);
							mH = mHEstimate * (Diagonal + Orthogonal + dxy.X + dxy.Y);
							break;
					}
					mCalcGrid[mNewLocation].F = mNewG + mH;

					mOpen.Push(mNewLocation);
					mCalcGrid[mNewLocation].Status = mOpenNodeValue;
				}

				mCloseNodeCounter++;
				mCalcGrid[mLocation].Status = mCloseNodeValue;
			}

			if (mFound)
			{
				mClose.Clear();
				var posX = end.X;
				var posY = end.Y;

				var fNodeTmp = mCalcGrid[mEndLocation];
				totalCost = fNodeTmp.G;
				mGrid[mEndLocation].lastPathFindingCost = totalCost;
				PathFinderNode fNode;
				fNode.F = fNodeTmp.F;
				fNode.G = fNodeTmp.G;
				fNode.H = 0;
				fNode.PX = fNodeTmp.PX;
				fNode.PY = fNodeTmp.PY;
				fNode.X = end.X;
				fNode.Y = end.Y;

				while (fNode.X != fNode.PX || fNode.Y != fNode.PY)
				{
					mClose.Add(fNode);
					posX = fNode.PX;
					posY = fNode.PY;
					var loc = posY * mGridX + posX;
					fNodeTmp = mCalcGrid[loc];
					fNode.F = fNodeTmp.F;
					fNode.G = fNodeTmp.G;
					mGrid[loc].lastPathFindingCost = fNodeTmp.G;
					fNode.H = 0;
					fNode.PX = fNodeTmp.PX;
					fNode.PY = fNodeTmp.PY;
					fNode.X = posX;
					fNode.Y = posY;
				}

				mClose.Add(fNode);

				return mClose;
			}
			return null;
		}

		internal class ComparePFNodeMatrix : IComparer<int>
		{
			protected PathFinderNodeFast[] mMatrix;

			public ComparePFNodeMatrix(PathFinderNodeFast[] matrix) => mMatrix = matrix;

			public int Compare(int a, int b)
			{
				if (mMatrix[a].F > mMatrix[b].F)
					return 1;
				else if (mMatrix[a].F < mMatrix[b].F)
					return -1;
				return 0;
			}

			public void SetMatrix(PathFinderNodeFast[] matrix)
			{
				mMatrix = matrix;
			}
		}
	}
}