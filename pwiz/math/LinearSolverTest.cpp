//
// LinearSolverTest.cpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2006 Louis Warschaw Prostate Cancer Center
//   Cedars Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#include "LinearSolver.hpp"
#include "util/unit.hpp"
#include <boost/numeric/ublas/matrix_sparse.hpp>
#include <boost/numeric/ublas/banded.hpp>
#include <iostream>


using namespace std;
using namespace pwiz::util;
using namespace pwiz::math;


namespace ublas = boost::numeric::ublas;


ostream* os_ = 0;


void testDouble()
{
    if (os_) *os_ << "testDouble()\n";

    LinearSolver<> solver;

    ublas::matrix<double> A(2,2);
    A(0,0) = 1; A(0,1) = 2;
    A(1,0) = 3; A(1,1) = 4;
   
    ublas::vector<double> y(2);
    y(0) = 5;
    y(1) = 11;

    ublas::vector<double> x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    unit_assert(x(0) == 1.);
    unit_assert(x(1) == 2.);
}


void testComplex()
{
    if (os_) *os_ << "testComplex()\n";

    LinearSolver<> solver;
    
    ublas::matrix< complex<double> > A(2,2);
    A(0,0) = 1; A(0,1) = 2;
    A(1,0) = 3; A(1,1) = 4;
   
    ublas::vector< complex<double> > y(2);
    y(0) = 5;
    y(1) = 11;

    ublas::vector< complex<double> > x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    unit_assert(x(0) == 1.);
    unit_assert(x(1) == 2.);
}

void testDoubleQR()
{
    if (os_) *os_ << "testDoubleQR()\n";

    LinearSolver<LinearSolverType_QR> solver;

    ublas::matrix<double> A(2,2);
    A(0,0) = 1.; A(0,1) = 2.;
    A(1,0) = 3.; A(1,1) = 4.;
   
    ublas::vector<double> y(2);
    y(0) = 5.;
    y(1) = 11.;

    ublas::vector<double> x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    if (os_) *os_ << x(0) << " - 1. = " << x(0) - 1. << endl;

    unit_assert_equal(x(0), 1., 1e-14);
    unit_assert_equal(x(1), 2., 1e-14);
}

/*
void testComplexQR()
{
    if (os_) *os_ << "testComplex()\n";

    LinearSolver<LinearSolverType_QR> solver;
    
    ublas::matrix< complex<double> > A(2,2);
    A(0,0) = 1; A(0,1) = 2;
    A(1,0) = 3; A(1,1) = 4;
   
    ublas::vector< complex<double> > y(2);
    y(0) = 5;
    y(1) = 11;

    ublas::vector< complex<double> > x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    unit_assert(x(0) == 1.);
    unit_assert(x(1) == 2.);
}
*/


void testSparse()
{
    if (os_) *os_ << "testSparse()\n";

    LinearSolver<> solver;

    ublas::mapped_matrix<double> A(2,2,4);
    A(0,0) = 1.; A(0,1) = 2.;
    A(1,0) = 3.; A(1,1) = 4.;
   
    ublas::vector<double> y(2);
    y(0) = 5.;
    y(1) = 11.;

    ublas::vector<double> x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    unit_assert_equal(x(0), 1., 1e-14);
    unit_assert_equal(x(1), 2., 1e-14);
}


/*
void testSparseComplex()
{
    if (os_) *os_ << "testSparseComplex()\n";

    LinearSolver<> solver;

    ublas::mapped_matrix< complex<double> > A(2,2,4);
    A(0,0) = 1.; A(0,1) = 2.;
    A(1,0) = 3.; A(1,1) = 4.;
   
    ublas::vector< complex<double> > y(2);
    y(0) = 5.;
    y(1) = 11.;

    ublas::vector< complex<double> > x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    unit_assert(norm(x(0)-1.) < 1e-14);
    unit_assert(norm(x(1)-2.) < 1e-14);
}
*/


void testBanded()
{
    if (os_) *os_ << "testBanded()\n";

    LinearSolver<> solver;

    ublas::banded_matrix<double> A(2,2,1,1);
    A(0,0) = 1.; A(0,1) = 2.;
    A(1,0) = 3.; A(1,1) = 4.;
   
    ublas::vector<double> y(2);
    y(0) = 5.;
    y(1) = 11.;

    ublas::vector<double> x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    unit_assert_equal(x(0), 1., 1e-14);
    unit_assert_equal(x(1), 2., 1e-14);
}


void testBandedComplex()
{
    if (os_) *os_ << "testBandedComplex()\n";

    LinearSolver<> solver;

    ublas::banded_matrix< complex<double> > A(2,2,1,1);
    A(0,0) = 1.; A(0,1) = 2.;
    A(1,0) = 3.; A(1,1) = 4.;
   
    ublas::vector< complex<double> > y(2);
    y(0) = 5.;
    y(1) = 11.;

    ublas::vector< complex<double> > x = solver.solve(A, y);

    if (os_) *os_ << "A: " << A << endl;
    if (os_) *os_ << "y: " << y << endl;
    if (os_) *os_ << "x: " << x << endl;

    unit_assert(norm(x(0)-1.) < 1e-14);
    unit_assert(norm(x(1)-2.) < 1e-14);
}


int main(int argc, char* argv[])
{
    try
    {
        if (argc>1 && !strcmp(argv[1],"-v")) os_ = &cout;
        if (os_) *os_ << "LinearSolverTest\n";

        testDouble();
        testComplex();
        testDoubleQR();
        //testComplexQR();
        testSparse();
        //testSparseComplex(); // lu_factorize doesn't like mapped_matrix<complex> 
        testBanded();
        testBandedComplex();
        return 0;
    }
    catch (exception& e)
    {
        cerr << e.what() << endl;
        return 1;
    }
}

