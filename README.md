# Simple Text Editor with AES Encryption

This is a simple text editor written in C# WPF created to explore AES data encryption (and for fun). 

The code is not entirely mine; I use [this code](https://github.com/NetkoNefarious/Text-Editor) as a foundation and added some functionalities.

## How It Works

1. **AES Encryption Key Generation**: 
   - When you open the program, it generates a random AES encryption key.
   - This key is written to the file `aesKeyInfo.json` in the main directory.
   - The keys are also encrypted(using another aes key) after generation for added security.

2. **File Saving and Encryption/Decryption**:
   - When you save a file, it is automatically encrypted using AES encryption.
   - These encrypted files can only be opened with this text editor.

## File sharing
  If you want to share encrypted file with someone, you need to send the encrypted file itself and `aesKeyInfo.json` file from main directory. Just drop keyInfo file in the main directory and open the encrypted file.
