# Soccer Offside Detection

Soccer Offside Detection is a project that aims to detect offside positions in soccer images using computer vision techniques. The project is built using .NET 8.0 and WPF for the user interface.

## Project Structure

```
┣ 📂src
┃ ┣ 📂Models
┃ ┃ ┣ Player.cs
┃ ┃ ┗ TeamType.cs
┃ ┣ 📂Services
┃ ┃ ┣ ImageProcessor.cs
┃ ┃ ┗ OffsideAnalyzer.cs
┃ ┣ 📂UI
┃ ┃ ┣ MainWindow.xaml
┃ ┃ ┗ MainWindow.xaml.cs
┃ ┗ Program.cs
┣ .gitignore
┣ LICENSE.md
┣ README.md
┣ SoccerOffsideDetection.csproj
┗ SoccerOffsideDetection.sln
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Visual Studio or Visual Studio Code

### Installation

1. Clone the repository:
    ```sh
    git clone https://github.com/yourusername/soccer-offside-detection.git
    ```
2. Navigate to the project directory:
    ```sh
    cd soccer-offside-detection
    ```
3. Restore the dependencies:
    ```sh
    dotnet restore
    ```

### Building the Project

To build the project, run the following command:
```sh
dotnet build
```

### Running the Project

To run the project, use the following command:
```sh
dotnet run --project SoccerOffsideDetection.csproj
```

## Usage

1. Launch the application.
2. Click on the "Import" button to select an image file.
3. Once the image is loaded, click on the "Process" button to detect players and analyze offside positions.

## Project Files

- **src/Models/Player.cs**: Defines the Player class with properties like Position, Team, `HasBall`, IsOffside, and IsAnnotated.
- **src/Models/TeamType.cs**: Defines the TeamType enum.
- **src/Services/ImageProcessor.cs**: Contains the ImageProcessor class that handles image processing and player detection.
- **src/UI/MainWindow.xaml.cs**: Contains the MainWindow class that handles the user interface logic.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License. See the [LICENSE](./LICENSE.md) file for details.

## Acknowledgements

- [Emgu CV](https://www.emgu.com/wiki/index.php/Main_Page) for the computer vision library.
- Microsoft for the .NET framework and WPF.