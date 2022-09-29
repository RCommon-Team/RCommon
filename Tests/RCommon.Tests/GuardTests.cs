
using System;
using NUnit.Framework;


namespace RCommon.Tests
{
    [TestFixture]
    public class GuardTests
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Against_Throws_Valid_Exception()
        {
            Guard.Against<ArgumentNullException>(true, string.Empty);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Against_Throws_Valid_Exception_WithMessage()
        {
            string message = "Exception Message";
            Guard.Against<ArgumentNullException>(true, message);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Against_Evaluates_Lambda_WithException()
        {
            Guard.Against<ArgumentNullException>
                        (
                            () => 1 == 1, "Guard check with lambda"
                        );
        }

        [Test]
        public void Against_Evaluates_Lambda_WithSuccess()
        {
            try 
	        {	        
		        Guard.Against<ArgumentNullException>
                    (
                        () => 1 != 1, "Guard check with lambda"
                    );
	        }
	        catch (ArgumentNullException)
	        {
		
		        Assert.Fail();
	        }
            catch(Exception)
            {

            }
             
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TypeOf_Throws_InvalidOperationException_When_Instance_Does_Not_Match_Type ()
        {
            Guard.TypeOf<InvalidOperationException>(new Exception(), "Guard check with TypeOf");
        }

        [Test]
        public void TypeOf_Does_Not_Throw_When_Instance_Does_Matches_Type()
        {

            try
            {
                Guard.TypeOf<InvalidOperationException>(new InvalidOperationException(), "Guard check with TypeOf");
            }
            catch(InvalidOperationException)
            {
                Assert.Fail();
            }
            catch(Exception)
            {

            }
        }
    }
}
