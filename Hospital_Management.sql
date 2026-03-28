create database Hospital;

use Hospital;


create table Staff
(
	Staff_ID varchar(20) primary key not null,
	Dept_ID varchar(20),
	Position varchar(20),
	Fname varchar(30) not null,
	Lname varchar(30) not null,
	constraint Jop_Position check (Position in ('Doctor','Nurse'))
);

create table Departments(
	Dept_ID varchar(20) primary key not null,
	Dept_Name varchar(100) not null,
	Dept_Floor int not null
);
create table Patients(
	 Patient_ID varchar(20) primary key not null,
	 Fname varchar(30) not null,
	 Lname varchar(30) not null,
	 gender varchar(10) not null,
	 patient_address varchar(50) not null,
	 phone varchar(20) not null,
	 constraint Check_gender check (gender in ('Male','Female'))
);

create table Appointments(
	Appointment_id varchar(20) primary key not null,
	Staff_ID varchar(20) not null,
	Patient_ID varchar(20),
	Appointment_date date not null,
	Appointment_status varchar(30),
	Reason varchar(200),
	constraint Check_Appointment_status check (Appointment_status in('Scheduled', 'Completed','Cancelled'))
);

create table Medical_Records(
	Medical_Records_ID varchar(20) primary key not null,
	Patient_ID varchar(20),
	Staff_ID varchar(20),
	Bill decimal(20,5),
	Treatment_Plan varchar(250),
	Diagnosis varchar(50) not null,
	Medication varchar(500),
	Visit_Date date not null
);

	--Relations--

	--Appointments are made by Staff--

alter table Appointments
add constraint FK_Appointment_Staff
foreign key (Staff_ID)
references Staff(Staff_ID);


    --Staff belongs to Departments--

alter table Staff 
add constraint Fk_Staff_Department
foreign key (Dept_ID)
references Departments(Dept_ID);

	--Appointments are attended by Patients-- 

alter table Appointments
add constraint FK_Appointment_Patient
foreign key (Patient_ID)
references Patients(Patient_ID);

	--Medical Records include Patients--

alter table Medical_Records  
add constraint Fk_Medical_Records_Patient
foreign key (Patient_ID)
references Patients(Patient_ID);

    --Medical Records include Staff--

alter table Medical_Records  
add constraint Fk_Medical_Records_Staff
foreign key (Staff_ID)
references Staff(Staff_ID);


       --insert data--


insert into Departments (Dept_ID, Dept_Name, Dept_Floor)
values 
('D001', 'Cardiology', 2),
('D002', 'Neurology', 3),
('D003', 'Pediatrics', 1);

select * from Departments;


insert into Staff (Staff_ID, Dept_ID, Position, Fname, Lname)
values
('S001', 'D001', 'Doctor', 'Ahmed', 'Hassan'),
('S002', 'D002', 'Doctor', 'Mona', 'Ali'),
('S003', 'D003', 'Nurse', 'Sara', 'Mahmoud');

select * from Staff;


insert into Patients (Patient_ID, Fname, Lname, gender, patient_address, phone)
values
('P001', 'Omar', 'Khaled', 'Male', 'Cairo, Egypt', '01012345678'),
('P002', 'Laila', 'Mostafa', 'Female', 'Giza, Egypt', '01098765432');

select * from Patients;


insert into Appointments (Appointment_id, Staff_ID, Patient_ID, Appointment_date, Appointment_status, Reason)
values
('A001', 'S001', 'P001', '2026-03-25', 'Scheduled', 'Routine Checkup'),
('A002', 'S002', 'P002', '2026-03-26', 'Scheduled', 'Headache Consultation');

select * from Appointments;


insert into Medical_Records (Medical_Records_ID, Patient_ID, Staff_ID, Bill, Treatment_Plan, Diagnosis, Medication, Visit_Date)
values
('MR001', 'P001', 'S001', 500.00, 'Follow-up in 2 weeks', 'High Blood Pressure', 'Amlodipine 5mg daily', '2026-03-25'),
('MR002', 'P002', 'S002', 300.00, 'MRI Scan recommended', 'Migraine', 'Paracetamol 500mg as needed', '2026-03-26');

select * from Medical_Records;
