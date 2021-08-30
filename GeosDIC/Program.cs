﻿using System;
using System.Collections.Generic;
using System.Linq;

/*
 * My own implementation of the .NET Core dependecy injector container. Trying to get a deeper understanding of it functioning.
 * See the different .stageX files to track the changes.
 * 
 * stage0 = Base Program without dependency inversion.
 * stage1 = In this version, we inverted the dipendencies and instanciated programmatically instead of using the *new* keyword. The process ist still manual though
 * stage2 = Here I have the first dependency instanciator (called serviceprovider in .NET Core). It is possible to instanciate objects and dependencies are injected automatically (no recursion yet)
 * stage3 = Here I have the dependency instanciator (called serviceprovider in .NET Core) and the dependency container (DIC). It is possible to instanciate objects and dependencies are injected automatically recursively
 * stage4 = Same as stage 3, but now further parameters can be provided as a list
 */

// Here i have all the functionalities plus I can spawn Singletons or Transients (so far only pseudo-code implemented as comment)

namespace GeosDIC
{
    class Program
    {
        static void Main(string[] args)
        {
            var dependencyContainer = new DependencyContainer();
            var dependencyInstanciator = new DependencyInstanciator(dependencyContainer); // Create Instanciator, ServiceProvider, ObjectSpawner, whatever you wanna call it 

            Console.WriteLine("Registering dependencies...");
            dependencyContainer.AddDependency<Car>();
            dependencyContainer.AddDependency<Stereo>();
            dependencyContainer.AddDependency<Heater>();
            dependencyContainer.AddDependency<CD>();


            Console.WriteLine("Instanciating the Car object and auto-injecting dependencies..."); Console.WriteLine("");
            dependencyInstanciator.Instanciate<Car>(
                new List<object> { "BMW" })
                ; // This spawns the car object and injects its dependencies and the parameters automatically

            Console.WriteLine(""); Console.WriteLine("");

            dependencyInstanciator.Instanciate<Car>(
                new List<object> { "Audi" }
                ); // This spawns the car object and injects its dependencies and the parameters automatically

            Console.ReadLine();
        }
    }

    // The dependency container lists the registered dependencies. The list is used to differentiate between dependencies and "normal" constructor Parameter at the moment of creation.
    public class DependencyContainer
    {
        public List<Type> dependencies;

        public DependencyContainer()
        {
            this.dependencies = new List<Type>();
        }

        // Adds dependencies to the container
        public void AddDependency<T>()
        {
            dependencies.Add(typeof(T));
        }

        // Gets dependencies from the container. In actuality it checks if the given type is registered as dependency, if not return a null object.
        public Type GetDependency(Type dependency)
        {
            return dependencies.Find(x => x.Name == dependency.Name);
        }

        // Add transient
        // - what is done right now basically
        // -> already solved

        // Adding singleton
        // - those are instanciated only once and is used for every parent
        // -> the dependency list is a multidimensional list with the true/false option isSingleton.
        // -> if its a singleton it gets istanciated straight away and stored in a dedicated list.
        // -> the instanciator dont instanciate if its a singleton, it takes the object itself.
    }

    // Automatically instanciate objects and injects dependency. Only the parent object needs to be given.
    public class DependencyInstanciator
    {
        private DependencyContainer dependencyContainer;
        private List<object> extraParameters;

        public DependencyInstanciator(DependencyContainer dependencyContainer)
        {
            this.dependencyContainer = dependencyContainer;
            this.extraParameters = new List<object>();
        }

        // Returns the instanciated object type given
        public object Instanciate(Type dependencyType)
        {
            // Check if and how many parameters the constructor needs. For simplicity only the first constructor overload is searched
            var constructorParameters = dependencyType.GetConstructors()[0].GetParameters(); // Return an array of parameters (the dependencies we need to inject)
            
            // If there are parameters, go trough them
            if(constructorParameters.Length > 0)
            {
                List<object> instanciatedConstructorParameters = new List<object>(); // here are stored the instanciated dependencies 
                // goes through all parameters, check if are known dependencies, if so instanciate objects off of those parameters and adds the objects to the list
                foreach (var parameter in constructorParameters) 
                {
                    // check if its a known dependency
                    Type dependency = dependencyContainer.GetDependency(parameter.ParameterType);
                    if(dependency != null)
                    {
                        instanciatedConstructorParameters.Add(Instanciate(dependency));
                    }
                    else // meaning there are parameters not known (not registered in the DIC)
                    {
                        // adds those to the list of instanciated objects
                        instanciatedConstructorParameters.AddRange(extraParameters);
                    }
                }
                var args = instanciatedConstructorParameters.Cast<object>().ToArray(); // Convert the object list to an array, used next in this format
                return Activator.CreateInstance(dependencyType, args); // Instanciate the wished class with the parameters (the dependencies)
            }
            // Else simply instanciate
            else
            {
                return Activator.CreateInstance(dependencyType);
            }
        }

        // overload method with generic call (for the first/top call, not really necessary but more efficient)
        public T Instanciate<T>()
        {
            return (T)Instanciate(typeof(T));
        }

        // overlaod method with an args list of further parameters beyond dependencies
        public T Instanciate<T>(List<object> args)
        {
            this.extraParameters = args;
            var obj = (T)Instanciate(typeof(T));
            this.extraParameters = new List<object>(); // Clears the extra parameters (should be  an effimeral field)
            return obj;
        }
    }



    // Example Classes *******************************************************************************************************************
    // Parent Object
    public class Car
    {
        public Stereo stereo;
        public Heater heater;

        public Guid carID { get; private set; }

        private string carName;

        public Car(Stereo stereo, Heater heater, String carName)
        {
            carID = Guid.NewGuid();
            this.carName = carName;
            this.stereo = stereo;
            this.heater = heater;
            Console.WriteLine($"Car created. Serial Nr: #{ carID } - Car Name: { carName }");
            Console.WriteLine($"and the Stereo #{ this.stereo.stereoID } and the Heater #{ this.heater.heaterID } where added.");
        }
    }

    // First Dependency
    public class Stereo
    {
        public Guid stereoID;
        public CD media;

        public Stereo(CD disc)
        {
            this.stereoID = Guid.NewGuid();
            this.media = disc;
            Console.WriteLine($"Stereo created. SerialNr: #{ this.stereoID }");
        }

    }

    // Second Dependency
    public class Heater
    {
        public Guid heaterID;

        public Heater()
        {
            this.heaterID = Guid.NewGuid();
            Console.WriteLine($"Heater created. SerialNr: #{ this.heaterID }");
        }
    }

    // Third Dependency (Recursive)
    public class CD
    {
        private Guid disc_ID;

        public CD()
        {
            this.disc_ID = Guid.NewGuid();
            Console.WriteLine($"Disc added. SerialNr: #{ this.disc_ID }");
        }

        public Guid GetID()
        {
            return disc_ID;
        }
    }
}
