Real sound effects go in this folder.

Name each file after the sound it replaces, exactly as it is spelled in
GameAudio.Sfx:

    Slash.wav   Shoot.wav   Hit.wav   EnemyDie.wav   Gem.wav
    Coin.wav    LevelUp.wav PlayerHurt.wav  Harvest.wav  Eat.wav

.wav, .ogg and .mp3 all work. Drop a file in and it plays the next time you
press Play -- there is nothing to wire up and no code to change, because
GameAudio looks the file up by name.

A name can also be a FOLDER holding several takes of the same sound, like
Slash/ does. One is then picked at random per play, never the same twice in a
row, so a sound you hear constantly does not turn into a loop.

A sound with no file here stays silent, unless "Play Generated Sounds" is
ticked on the GameAudio object, which brings back the maths-made placeholder.

Keep the clips short (a hit is about 0.1s, a pickup about 0.2s) and check the
licence of anything you download before shipping it.
