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
//  Heavily modified by Ramiro Oliva to make it compatible with entity routes (Country to Country or Province to Province)

using UnityEngine;
using System;
using System.Collections.Generic;

namespace WorldMapStrategyKit.PathFinding
{
	public delegate float OnAdminEntityCross(int adminEntityIndex);

	public class PathFinderAdminEntity
	{
		// Heap variables are initializated to default, but I like to do it anyway
		private AdminEntity[] mEntities = null;
		private PriorityQueueB<int> mOpen = null;
		private List<PathFinderNodeAdmin> mClose = new();
		private float mHEstimate = 2;
		private float mSearchLimit = 2000;
		private PathFinderNodeAdmin[] mCalcGrid = null;
		private byte mOpenNodeValue = 1;
		private byte mCloseNodeValue = 2;
		private OnAdminEntityCross mOnAdminEntityCross = null;

		//Promoted local variables to member variables to avoid recreation between calls
		private float mH;
		private int mLocation;
		private int mNewLocation;
		private int mCloseNodeCounter;
		private bool mFound;
		private ushort mEndLocation;
		private float mNewG;

		public PathFinderAdminEntity(AdminEntity[] entities)
		{
			if (entities == null)
				throw new Exception("entities cannot be null");

			mEntities = entities;

			if (mCalcGrid == null || mCalcGrid.Length != mEntities.Length)
				mCalcGrid = new PathFinderNodeAdmin[mEntities.Length];

			mOpen = new PriorityQueueB<int>(new ComparePFNodeMatrix(mCalcGrid));
		}

		public float HeuristicEstimate { get => mHEstimate; set => mHEstimate = value; }

		public float SearchLimit { get => mSearchLimit; set => mSearchLimit = value; }

		public List<int> GetExaminedEntities()
		{
			var entities = new List<int>(mCalcGrid.Length);
			for (var k = 0; k < mCalcGrid.Length; k++)
				if (mCalcGrid[k].Status == mOpenNodeValue)
					entities.Add((int)mCalcGrid[k].Parent);
			return entities;
		}

		public OnAdminEntityCross OnAdminEntityCross
		{
			get => mOnAdminEntityCross;
			set => mOnAdminEntityCross = value;
		}

		public List<PathFinderNodeAdmin> FindPath(int start, int end)
		{
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

			mLocation = (ushort)start;
			mEndLocation = (ushort)end;
			mCalcGrid[mLocation].G = 0;
			mCalcGrid[mLocation].F = mHEstimate;
			mCalcGrid[mLocation].Parent = (ushort)start;
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

				if (mCloseNodeCounter > mSearchLimit)
					return null;

				//Lets calculate each successors
				var maxi = mEntities[mLocation].neighbours != null
					? mEntities[mLocation].neighbours.Length
					: 0;
				for (var i = 0; i < maxi; i++)
				{
					mNewLocation = mEntities[mLocation].neighbours[i];

					// Unbreakeable?
					if (!mEntities[mNewLocation].canCross)
						continue;

					// Check custom validator
					var gridValue = mEntities[mNewLocation].crossCost;
					if (mOnAdminEntityCross != null)
					{
						var customValue = mOnAdminEntityCross(mNewLocation);
						if (customValue < 0)
							continue;
						gridValue += customValue;
					}

					mNewG = mCalcGrid[mLocation].G + gridValue;

					//Is it open or closed?
					if (mCalcGrid[mNewLocation].Status == mOpenNodeValue ||
					    mCalcGrid[mNewLocation].Status ==
					    mCloseNodeValue) // The current node has less code than the previous? then skip this node
						if (mCalcGrid[mNewLocation].G <= mNewG)
							continue;

					mCalcGrid[mNewLocation].Parent = mLocation;
					mCalcGrid[mNewLocation].G = mNewG;

					var newLocationPos = mEntities[mNewLocation].center;
					var dv = newLocationPos - mEntities[end].center;
					if (dv.x < 0)
						dv.x = -dv.x;
					dv.x = Mathf.Min(dv.x, 1f - dv.x);
					var dist = dv.sqrMagnitude * 1000f;
					mH = mHEstimate * dist;

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
				var fNodeTmp = mCalcGrid[end];
				PathFinderNodeAdmin fNode;
				fNode.F = fNodeTmp.F;
				fNode.G = fNodeTmp.G;
				fNode.Parent = fNodeTmp.Parent;
				fNode.Index = end;
				fNode.Status = 0;

				while (fNode.Index != fNode.Parent)
				{
					mClose.Add(fNode);
					var pos = fNode.Parent;
					fNodeTmp = mCalcGrid[pos];
					fNode.F = fNodeTmp.F;
					fNode.G = fNodeTmp.G;
					fNode.Parent = fNodeTmp.Parent;
					fNode.Index = pos;
				}

				mClose.Add(fNode);

				return mClose;
			}
			return null;
		}

		internal class ComparePFNodeMatrix : IComparer<int>
		{
			protected PathFinderNodeAdmin[] mMatrix;

			public ComparePFNodeMatrix(PathFinderNodeAdmin[] matrix) => mMatrix = matrix;

			public int Compare(int a, int b)
			{
				if (mMatrix[a].F > mMatrix[b].F)
					return 1;
				else if (mMatrix[a].F < mMatrix[b].F)
					return -1;
				return 0;
			}

			public void SetMatrix(PathFinderNodeAdmin[] matrix)
			{
				mMatrix = matrix;
			}
		}
	}
}