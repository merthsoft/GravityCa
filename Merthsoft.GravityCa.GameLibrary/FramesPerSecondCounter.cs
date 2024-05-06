using Microsoft.Xna.Framework;

namespace Merthsoft.Moose.Merthsoft.GravityCa.GameLibrary;
// https://github.com/craftworkgames/MonoGame.Extended/blob/develop/src/cs/MonoGame.Extended/FramesPerSecondCounter.cs
// https://github.com/craftworkgames/MonoGame.Extended/blob/develop/LICENSE
/*
The MIT License (MIT)

Copyright (c) 2015-2020:
- Dylan Wilson (https://github.com/dylanwilson80)
- Lucas Girouard-Stranks (https://github.com/lithiumtoast)

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

public class FramesPerSecondCounter
{
    private static readonly TimeSpan _oneSecondTimeSpan = new TimeSpan(0, 0, 1);
    private int _framesCounter;
    private TimeSpan _timer = _oneSecondTimeSpan;

    public FramesPerSecondCounter()
    {
    }

    public int FramesPerSecond { get; private set; }

    public void Update(GameTime gameTime)
    {
        _timer += gameTime.ElapsedGameTime;
        if (_timer <= _oneSecondTimeSpan)
            return;

        FramesPerSecond = _framesCounter;
        _framesCounter = 0;
        _timer -= _oneSecondTimeSpan;
    }

    public void Draw(GameTime gameTime)
    {
        _framesCounter++;
    }
}
