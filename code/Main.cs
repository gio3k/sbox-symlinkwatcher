using System.Collections.Generic;
using Editor;
using Sandbox;

namespace SymlinkWatcher;

public static class Main
{
	private static readonly List<WatchedProject> WatchedProjects = new();

	[Menu( "Editor", "File/Watch Symlinks", "abc" )]
	public static void WatchSymlinks() => Update();

	[Event( "localaddons.changed" )]
	private static void LocalAddonsChanged() => Update();

	[Event( "editor.created" )]
	private static void EditorCreated( EditorMainWindow window ) => Update();

	static Main() => Update();

	private static void Update()
	{
		StopWatching();

		foreach ( var project in Internal.GetAllLocalProjects() ) ProcessLocalProject( project );
	}

	private static void StopWatching()
	{
		foreach ( var watchedProject in WatchedProjects ) watchedProject.Dispose();

		WatchedProjects.Clear();
	}

	private static void ProcessLocalProject( LocalProject project )
	{
		if ( !project.Active )
			return; // Don't bother with inactive projects

		WatchedProjects.Add( new WatchedProject( project ) );
	}
}
