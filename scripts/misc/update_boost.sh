#!/usr/bin/env bash
set -e
cd "$(dirname "$0")"

# The Boost version to download:
boost_version=1.76.0

# Create a temporary directory to work in:
tmp_dir=$(mktemp -d -t ci-XXXXXXXXXX)
cd ${tmp_dir}

# Download and unpack the boost tarball:
base=boost_$(tr "." "_" <<< ${boost_version})

echo "Downloading Boost tarball..." > /dev/stderr
curl -0L https://boostorg.jfrog.io/artifactory/main/release/${boost_version}/source/${base}.tar.bz2 \
     -o ${base}.tar.bz2

echo "Unpacking Boost tarball..." > /dev/stderr
tar -xjf ${base}.tar.bz2

# Remove the directories suggested by Matt, then repack:
# Note that the libs/test directory is needed to avoid a build error.
echo "Trimming Boost..." > /dev/stderr
rm -r ${base}/doc
echo "Re-packing Boost into tarball..." > /dev/stderr
tar -cjSf ${base}.tar.bz2 ${base}

# Add to the pwiz repo:
cd - > /dev/null
rm ../../libraries/boost_*.tar.bz2
cp ${tmp_dir}/${base}.tar.bz2 ../../libraries

# Update Jamroot.jam:
echo "Updating Jamroot.jam..." > /dev/stderr
echo "  -> Changes:" > /dev/stderr
sed -i "" -re "s/boost(_[0-9]+)+/${base}/gw /dev/stderr" ../../Jamroot.jam

echo "DONE!" > /dev/stderr
