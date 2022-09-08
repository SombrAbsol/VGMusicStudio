﻿using System.Diagnostics;
using System.Threading;

namespace Kermalis.VGMusicStudio.Core.Util;

// Credit to ipatix
// TODO: High resolution timer instead.
internal sealed class TimeBarrier
{
	private readonly Stopwatch _sw;
	private readonly double _timerInterval;
	private readonly double _waitInterval;
	private double _lastTimeStamp;
	private bool _started;

	public TimeBarrier(double ticksPerSecond)
	{
		_waitInterval = 1.0 / ticksPerSecond;
		_started = false;
		_sw = new Stopwatch();
		_timerInterval = 1.0 / Stopwatch.Frequency;
	}

	public void Wait()
	{
		if (!_started)
		{
			return;
		}
		double totalElapsed = _sw.ElapsedTicks * _timerInterval;
		double desiredTimeStamp = _lastTimeStamp + _waitInterval;
		double timeToWait = desiredTimeStamp - totalElapsed;
		if (timeToWait > 0)
		{
			Thread.Sleep((int)(timeToWait * 1_000));
		}
		_lastTimeStamp = desiredTimeStamp;
	}

	public void Start()
	{
		if (_started)
		{
			return;
		}
		_started = true;
		_lastTimeStamp = 0;
		_sw.Restart();
	}

	public void Stop()
	{
		if (!_started)
		{
			return;
		}
		_started = false;
		_sw.Stop();
	}
}
