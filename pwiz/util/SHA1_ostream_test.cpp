//
// SHA1_ostream_test.cpp 
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2007 Spielberg Family Center for Applied Proteomics
//   Cedars-Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#include "SHA1_ostream.hpp"
#include "unit.hpp"
#include "boost/iostreams/flush.hpp"
#include <iostream>
#include <sstream>
#include <fstream>


using namespace pwiz::util;
using namespace std;


ostream* os_ = 0;


const char* textBrown_ = "The quick brown fox jumps over the lazy dog";
const char* hashBrown_ = "2fd4e1c67a2d28fced849ee1bb76e7391b93eb12";


void test()
{
    ostringstream oss;
    SHA1_ostream sha1os(oss);

    sha1os << textBrown_ << flush;
    string hash = sha1os.hash();
    sha1os.explicitFlush();

    if (os_) *os_ << "str: " << oss.str() << endl
                  << "hash: " << hash << endl;

    unit_assert(hash == hashBrown_);
    unit_assert(sha1os.hash() == hashBrown_);

    sha1os << textBrown_ << flush;
    sha1os.explicitFlush();

    hash = sha1os.hash();
   
    if (os_) *os_ << "str: " << oss.str() << endl
                  << "hash: " << hash << endl;

    string hash2 = SHA1Calculator::hash(string(textBrown_) + textBrown_);
    unit_assert(sha1os.hash() == hash2);
}


void testFile()
{
    string filename = "SHA1_ostream.temp.txt";
    ofstream ofs(filename.c_str(), ios::binary); // binary necessary on Windows to avoid \n -> \r\n translation     
    SHA1_ostream sha1os(ofs);

    sha1os << textBrown_ << '\n' << textBrown_ << flush;
    string hashStream = sha1os.hash();

    sha1os.explicitFlush();
    string hashFile = SHA1Calculator::hashFile(filename);

    if (os_) *os_ << "stream: " << hashStream << endl
                  << "file  : " << hashFile << endl; 

    unit_assert(hashStream == hashFile);
    unit_assert(hashStream == "a159e6cde4e50e51713700d1fe4d0ce553eace87");
    system(("rm " + filename).c_str());
}


int main(int argc, char* argv[])
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        test();
        testFile();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
    }
    catch (...)
    {
        cerr << "Caught unknown exception.\n";
    }

    return 1;
}

