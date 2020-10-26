# AELBackup
Alternative lancher for the Steam version of Atelier Escha &amp; Logy DX

## What it does 
A bug exists in the Steam release of Atelier Escha & Logy: Alchemists of the Dusk Sky that causes your system save data to be lost. This causes your volume, lanugage, text skip, and, more importantly, bonus and progress flags to be lost. This small program serves as a work around solution to make sure you do not lose your system settings, bonus data, or progression flags.

## Installation
1. Download the .zip file found in the [Releases](https://github.com/Error503/AELBackup/releases) section
2. Extract the contents of the zip file to the folder of your choice
3. Open Steam and navigate to your Library
4. Click on "Add a Game" and then "Add a Non-Steam Game" located in the bottom left corner of your Library. A small window will appear
5. Click on the "Browse" button and navigate to the folder to which you just extracted the contents of the zip file
6. Click on AELBackup.exe and click "Okay" in the file selection window and then "Add Selected Program" in the Steam popup window
7. Run the game like any other game in your Steam Library
8. On the first run of the program, you will be asked to provide the file path of the game if you did not specify it in the optional arguments.
    - This value is the path to the "Atelier_Escha_and_Logy_EN.exe" file on your system which you can get by browsing the Local Files of the original game from your Steam library

## Command Line Arguments
There are a few command line arguments that you can specify to customize functionality. You can specify these options in the Properties window for the game in Steam. These settings are optional. If you do not specify the file path to the game as an argument, you will be prompted to provide it on the first run of the program. It will then be saved in a file along with the backups.

- `d` or `directory`: The folder under the save game directory in which backups of the system data will be saved. Defaults to `backups`.
- `c` or `count`: The maximum number of system data backups to be kept at any one time. Defaults to `5`.
- `g` or `game`: The full file path to the game's executable, usually "Atelier_Escha_and_Logy_EN.exe". No default value provided, you will be asked to provide it on the first run.

## Uninstalling
To unistall this program, delete the application file or folder that was extracted during the installation process

To delete the generated backups, delete the folder to which backups were being saved under in the game's ave data directory. By default this is `C:\Users\[UserName]\Documents\KoeiTecmo\Atelier Escha and Logy DX\SAVEDATA\backups`
