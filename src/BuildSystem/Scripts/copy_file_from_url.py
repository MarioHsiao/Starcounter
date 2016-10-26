import argparse
import os
import urllib.request
import shutil

# Declaring argument parsing rules.
parser = argparse.ArgumentParser(description='Download a file by URL if needed.')
parser.add_argument("--fileurl", required=True, help="Link to the file that should be downloaded (ftp://, http://, https://, etc).")
parser.add_argument("--destdir", required=True, help="Destination directory to where the file should be copied (name is preserved).")
parser.add_argument("--newfilename", required=True, help="The new local file name that should be given to the downloaded file.")
my_args = parser.parse_args()

# Getting file name from URL.
orig_file_name = my_args.fileurl.rsplit('/', 1)[-1]
orig_file_path = os.path.join(my_args.destdir, orig_file_name)

# Creating destination directory.
if not os.path.exists(my_args.destdir):
    os.makedirs(my_args.destdir)

print("Trying to download file from '{0}' into '{1}'.".format(
    my_args.fileurl, orig_file_path))

# Checking if file exists already in the directory.
if os.path.isfile(orig_file_path):
    print("File already cached locally. Skipping download.")
else:
    print("Starting download...")
    # Allowing maximum connection waiting time for 10 seconds.
    with urllib.request.urlopen(my_args.fileurl, None, 10) as response:
        file_data = response.read()
        with open(orig_file_path, "wb") as f:
            f.write(file_data)

    print("File successfully downloaded!")

# Now we need to copy the local file accordingly.
dest_file_path = os.path.join(my_args.destdir, my_args.newfilename)

# Checking if file exists.
if os.path.isfile(dest_file_path):

    # Checking if files have different modification time.
    if os.stat(orig_file_path).st_mtime != os.stat(dest_file_path).st_mtime:

        # Copying file preserving the metadata (dates, etc).
        shutil.copy2(orig_file_path, dest_file_path)
        print("Copied (diff date) cached file '{0}' to '{1}' inside directory '{2}'.".format(
            orig_file_name, my_args.newfilename, my_args.destdir))
    else:
        print("Destination file name is the same as original (time of last modification), not copying.")
else:
    # Copying file preserving the metadata (dates, etc).
    shutil.copy2(orig_file_path, dest_file_path)
    print("Copied (from scratch) cached file '{0}' to '{1}' inside directory '{2}'.".format(
        orig_file_name, my_args.newfilename, my_args.destdir))
