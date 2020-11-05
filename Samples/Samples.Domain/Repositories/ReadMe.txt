Nothing in here because we are using a Generic Repository provided by RCommon. We could easily 
add subtypes of the IEagerFetchingRepository to enhance testability. Additionally, adding a concrete
repository allows us to easily extend our repository interface without changing the correctness of
the application. This is known as the Liskov substitution principle https://en.wikipedia.org/wiki/Liskov_substitution_principle 