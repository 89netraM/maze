using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;

namespace Maze.Web.Models;

/// <param name="Size">The size of the maze.</param>
/// <param name="EntryCount">The number of entry points to the maze.</param>
/// <param name="Seed">Seed that produces the same maze each time.</param>
public record MazeConfig(
	[Range(1, Int32.MaxValue)]
		uint Size = 9,
	[Range(1, Int32.MaxValue)]
		uint EntryCount = 3,
	int? Seed = null);
