﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace TJAPlayer3.C曲リストノードComparers
{
	internal sealed class C曲リストノードComparerGenre : IComparer<C曲リストノード>
	{
		public C曲リストノードComparerGenre(int order)
		{
			this.order = order;
		}

		public int Compare(C曲リストノード n1, C曲リストノード n2)
		{
			return CStrGenreToNum.Genre(n1.strGenre, order).CompareTo(CStrGenreToNum.Genre(n2.strGenre, order));
		}

		private readonly int order;
	}
}