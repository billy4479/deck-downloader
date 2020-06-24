# deck-downloader
This is a simple .NET Console Application which helps you downloading Yu-Gi-Oh cards (both data as a JSON and the image).

## Building
You should be able to build this app on Linux, Windows and MacOS (not tested)
```bash
git clone https://github.com/billy4479/deck-downloader.git --recurse
cd deck-downloader
dotner build
```

## Usage
This app expects as input a ```.ydk``` file, which is just a text file llisting the ids of the cards in your deck. [Sample](deck-sample.ydk)

You can easly generate one using applications like YgoDeck app available on Android.

The output of this app will be saved in ```./downloaded```

## License
This software is under MIT License. For further information view the [LICENSE file](LICENSE).