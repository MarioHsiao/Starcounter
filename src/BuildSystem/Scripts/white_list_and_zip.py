import os
import glob
import subprocess
import sys
import argparse
import shutil
import argparse

# Declaring argument parsing rules.
parser = argparse.ArgumentParser(description="Clean given directory based on white list and zipping clean directory.")
parser.add_argument("--whitelist", required=True, help="Path to white list file.")
parser.add_argument("--zipname", required=True, help="Name of the produced ZIP file.")
parser.add_argument("--inputdir", required=True, help="Path to input directory.")
parser.add_argument("--producezip", required=True, help="Should the ZIP file be produced.")
my_args = parser.parse_args()

# Assigning local variables.
white_list_file_path = my_args.whitelist
zip_archive_name = my_args.zipname
input_dir = my_args.inputdir
producezip = my_args.producezip

print("-- Cleaning directory \"{0}\" according to white list \"{1}\" into ZIP file \"{2}\"".format(input_dir, white_list_file_path, zip_archive_name))

# Adding a trailing slash if its not there already.
input_dir = os.path.join(input_dir, "")

# Reading the white list file.
allowed_files = []
allowed_dirs = []
with open(white_list_file_path, "r") as lines:
  for line in lines:
    if not line.startswith("#") and not line.startswith("//") and not line.isspace():
      l = line.rstrip()
      if l.endswith("/") or l.endswith("\\"):
        allowed_dirs.append(l[:-1])
      else:
        allowed_files.append(l)

# Creating relative paths based on cleaning directory.
relative_paths = []
for root, dirnames, filenames in os.walk(input_dir):
  for file in filenames:
    path_to_file = os.path.join(root, file).replace(input_dir, "")
    relative_paths.append(path_to_file)

# Checking every relative path for white list and deleting files that are not allowed.
for relative_path in relative_paths:
  to_remove = True
  for allowed_dir in allowed_dirs:
    if relative_path.startswith(allowed_dir):
      to_remove = False
  for allowed_file in allowed_files:
    if relative_path.startswith(allowed_file):
      to_remove = False
  if to_remove:
    full_path_to_file = os.path.join(input_dir, relative_path)
    print("-- Removing file: {0}".format(full_path_to_file))
    os.remove(full_path_to_file)

# Removing empty directories.
for root, dirnames, filenames in os.walk(input_dir, False):
  if not os.listdir(root):
    os.rmdir(root)
    print("-- Removed empty dir: {0}".format(root))

# Checking if ZIP should be produced.
if producezip.lower() == "true":

  print("-- Whitelisting finished, ZIPing directory: {0}".format(input_dir))
  path_to_zip = os.path.join(input_dir, "..", zip_archive_name)
  shutil.make_archive(path_to_zip, "zip", input_dir)
  print("-- Created ZIP file: {0}".format(path_to_zip + ".zip"))

  # Publishing ZIP as TeamCity artifact.
  print("##teamcity[publishArtifacts '{0}']".format(path_to_zip + ".zip"))

sys.exit(0)