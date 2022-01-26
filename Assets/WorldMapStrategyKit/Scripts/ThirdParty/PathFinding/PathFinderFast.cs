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
//  Some modifications by Kronnect Games to reuse grid buffers between calls and to allow different grid configurations in same grid array (uses bitwise differentiator)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit.PathFinding
{
	public delegate float OnCellCross(int location);

	public class PathFinderFast : IPathFinder
	{
		// Heap variables are initializated to default, but I like to do it anyway
		private byte[] mGrid;
		private PriorityQueueB<int> mOpen;
		private List<PathFinderNode> mClose = new();
		private HeuristicFormula mFormula = HeuristicFormula.Manhattan;
		private bool mDiagonals = true;
		private float mHEstimate = 2;
		private int mMaxSteps = 2000;
		private float mMaxSearchCost = 100000;
		private PathFinderNodeFast[] mCalcGrid;
		private byte mOpenNodeValue = 1;
		private byte mCloseNodeValue = 2;
		private byte mGridBit = 1;

		private float[] mCustomCosts;
		// optional values for custom validation

		//Promoted local variables to member variables to avoid recreation between calls
		private float mH;
		private int mLocation;
		private int mNewLocation;
		private ushort mLocationX;
		private ushort mLocationY;
		private ushort mNewLocationX;
		private ushort mNewLocationY;
		private int mCloseNodeCounter;
		private ushort mGridX;
		private ushort mGridY;
		private ushort mGridXMinus1;
		private ushort mGridYLog2;
		private bool mFound;

		private sbyte[,] mDirection = new sbyte[8, 2]
		{
			{ 0, -1 },
			{ 1, 0 },
			{ 0, 1 },
			{ -1, 0 },
			{
				1,
				-1
			},
			{
				1,
				1
			},
			{
				-1,
				1
			},
			{
				-1,
				-1
			}
		};

		private int mEndLocation;
		private float mNewG;

		public PathFinderFast(byte[] grid, byte gridBit, int gridWidth, int gridHeight,
			float[] customCosts)
		{
			if (grid == null)
				throw new Exception("Grid cannot be null");

			mGrid = grid;
			mCustomCosts = customCosts;
			mGridBit = gridBit;
			mGridX = (ushort)gridWidth;
			mGridY = (ushort)gridHeight;
			mGridXMinus1 = (ushort)(mGridX - 1);
			mGridYLog2 = (ushort)Math.Log(mGridX, 2);

			// This should be done at the constructor, for now we leave it here.
			if (Math.Log(mGridX, 2) != (int)Math.Log(mGridX, 2) ||
			    Math.Log(mGridY, 2) != (int)Math.Log(mGridY, 2))
				throw new Exception("Invalid Grid, size in X and Y must be power of 2");

			if (mCalcGrid == null || mCalcGrid.Length != mGridX * mGridY)
				mCalcGrid = new PathFinderNodeFast[mGridX * mGridY];

			mOpen = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));
		}

		public void SetCalcMatrix(byte[] grid, byte gridBit)
		{
			if (grid == null)
				throw new Exception("Grid cannot be null");
			if (
				grid.Length !=
				mGrid.Length) // mGridX != (ushort) (mGrid.GetUpperBound(0) + 1) || mGridY != (ushort) (mGrid.GetUpperBound(1) + 1))
				throw new Exception(
					"SetCalcMatrix called with matrix with different dimensions. Call constructor instead.");
			mGrid = grid;
			mGridBit = gridBit;

			Array.Clear(mCalcGrid, 0, mCalcGrid.Length);
			var comparer = (ComparePFNodeMatrix)mOpen.comparer;
			comparer.SetMatrix(mCalcGrid);
		}

		public void SetCustomRouteMatrix(float[] newRouteMatrix)
		{
			if (newRouteMatrix != null && newRouteMatrix.Length != mCustomCosts.Length)
				throw new Exception("SetCustomRouteMatrix called with matrix with different dimensions.");
			mCustomCosts = newRouteMatrix;
		}

		public HeuristicFormula Formula { get => mFormula; set => mFormula = value; }

		public bool Diagonals
		{
			get => mDiagonals;
			set
			{
				mDiagonals = value;
				if (mDiagonals)
					mDirection = new sbyte[8, 2]
					{
						{ 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, -1 }, { 1, 1 }, { -1, 1 },
						{ -1, -1 }
					};
				else
					mDirection = new sbyte[4, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
			}
		}

		public bool HeavyDiagonals { get; set; }

		public float HeuristicEstimate { get => mHEstimate; set => mHEstimate = value; }

		public float MaxSearchCost { get => mMaxSearchCost; set => mMaxSearchCost = value; }

		public int MaxSteps { get => mMaxSteps; set => mMaxSteps = value; }

		public List<Vector2> GetExaminedPlaces()
		{
			var places = new List<Vector2>(mCalcGrid.Length);
			for (var k = 0; k < mCalcGrid.Length; k++)
				if (mCalcGrid[k].Status == mOpenNodeValue)
				{
					var x = (float)mCalcGrid[k].PX / mGridX - 0.5f;
					var y = (float)mCalcGrid[k].PY / mGridY - 0.5f;
					places.Add(new Vector2(x, y));
				}
			return places;
		}

		public OnCellCross OnCellCross { get; set; }

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

			mLocation = (start.Y << mGridYLog2) + start.X;
			mEndLocation = (end.Y << mGridYLog2) + end.X;
			mCalcGrid[mLocation].G = 0;
			mCalcGrid[mLocation].F = mHEstimate;
			mCalcGrid[mLocation].PX = (ushort)start.X;
			mCalcGrid[mLocation].PY = (ushort)start.Y;
			mCalcGrid[mLocation].Status = mOpenNodeValue;

			mOpen.Push(mLocation);

			while (mOpen.Count > 0)
			{
				mLocation = mOpen.Pop();

				//Is it in closed list? means this node was already processed
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

				mLocationX = (ushort)(mLocation & mGridXMinus1);
				mLocationY = (ushort)(mLocation >> mGridYLog2);

				//Lets calculate each successors
				var maxi = mDiagonals ? 8 : 4;
				for (var i = 0; i < maxi; i++)
				{
					mNewLocationX = mLocationX == 0 && mDirection[i, 0] < 0
						? (ushort)(mGridX - 1)
						: (ushort)(mLocationX + mDirection[i, 0]);
					mNewLocationY = (ushort)(mLocationY + mDirection[i, 1]);

					if (mNewLocationY >= mGridY)
						continue;

					if (mNewLocationX >= mGridX)
						mNewLocationX = 0;

					mNewLocation = (mNewLocationY << mGridYLog2) + mNewLocationX;

					// mGrid contains bitwise terrain capability (1=any, 2=only ground, 4=only water)
					var gridValue = (mGrid[mNewLocation] & mGridBit) > 0 ? 0.01f : 0;
					if (gridValue == 0)
						continue;

					// Check custom validator
					if (mCustomCosts != null)
					{
						var customValue = mCustomCosts[mNewLocation];
						if (customValue < 0 && OnCellCross != null)
							customValue = OnCellCross(mNewLocation);
						if (customValue == 0)
							continue;
						if (customValue < 0)
							customValue = 0;
						gridValue += customValue;
					}

					if (HeavyDiagonals && i > 3)
						mNewG = mCalcGrid[mLocation].G + gridValue * 2.41f;
					else
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
					dist = Math.Min(dist, mGridX - dist);
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
				var fNodeTmp = mCalcGrid[(end.Y << mGridYLog2) + end.X];
				totalCost = fNodeTmp.G;
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
					var posX = fNode.PX;
					var posY = fNode.PY;
					fNodeTmp = mCalcGrid[(posY << mGridYLog2) + posX];
					fNode.F = fNodeTmp.F;
					fNode.G = fNodeTmp.G;
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
				if (mMatrix[a].F < mMatrix[b].F)
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