//
// Stats.cpp
//
//
// Darren Kessner <Darren.Kessner@cshs.org>
//
// Copyright 2006 Louis Warschaw Prostate Cancer Center
//   Cedars Sinai Medical Center, Los Angeles, California  90048
//   Unauthorized use or reproduction prohibited
//


#include "Stats.hpp"
#include <iostream>


using namespace std;


namespace pwiz {
namespace math {


class Stats::Impl
{
    public:

    Impl(const Stats::data_type& data);

    Stats::vector_type mean() const;
    Stats::matrix_type covariance() const;
    Stats::matrix_type meanOuterProduct() const;

    private:
    unsigned int D_; // dimension of the data
    int N_; // number of data points

    Stats::vector_type sumData_;
    Stats::matrix_type sumOuterProducts_;

    void computeSums(const Stats::data_type& data);
};


Stats::Impl::Impl(const Stats::data_type& data)
:   D_(0),
    N_(data.size())
{
    computeSums(data);
}


Stats::vector_type Stats::Impl::mean() const
{
    return sumData_/N_; 
}


Stats::matrix_type Stats::Impl::meanOuterProduct() const
{
    return sumOuterProducts_/N_;
}


Stats::matrix_type Stats::Impl::covariance() const
{
    Stats::vector_type m = mean(); 
    return meanOuterProduct() - outer_prod(m, m); 
}
    
    
void Stats::Impl::computeSums(const Stats::data_type& data)
{
    if (data.size()>0) D_ = data[0].size();
    sumData_ = Stats::vector_type(D_);
    sumOuterProducts_ = Stats::matrix_type(D_, D_);

    sumData_.clear();
    sumOuterProducts_.clear();

    for (Stats::data_type::const_iterator it=data.begin(); it!=data.end(); ++it)
    {
        if (it->size() != D_)
        {
            ostringstream message;
            message << "[Stats::Impl::computeSums()] " << D_ << "-dimensional data expected: " << *it; 
            throw runtime_error(message.str());
        } 

        sumData_ += *it;
        sumOuterProducts_ += outer_prod(*it, *it);
    }
}


Stats::Stats(const Stats::data_type& data) : impl_(new Stats::Impl(data)) {}
Stats::~Stats() {} // auto destruction of impl_
Stats::vector_type Stats::mean() const {return impl_->mean();}
Stats::matrix_type Stats::meanOuterProduct() const {return impl_->meanOuterProduct();}
Stats::matrix_type Stats::covariance() const {return impl_->covariance();}


} // namespace math
} // namespace pwiz

