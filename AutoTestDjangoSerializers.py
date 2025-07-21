import inspect
import json
from datetime import datetime

import pytest
from django.db.models import Model
from faker import Faker
from rest_framework import serializers


@pytest.mark.django_db(transaction=True)
def get_fake_model_instance(model):
    fake = Faker()
    input_fields = {}

    for field in model._meta.fields:
        field_type = field.get_internal_type()

        if field_type in ["ForeignKey", "OneToOneField"]:
            input_fields[field.name] = get_fake_model_instance(field.related_model)

        elif field_type == "BooleanField":
            input_fields[field.name] = fake.boolean()

        elif field_type in ["PositiveIntegerField", "DecimalField", "FloatField"]:
            input_fields[field.name] = fake.random_int()

        elif field_type == "CharField":
            input_fields[field.name] = fake.pystr(
                min_chars=1, max_chars=field.max_length
            )

    return model.objects.get_or_create(**input_fields)[0]


class SerializerTest:
    def __init__(self, serializer_file):
        self.serializer_file = serializer_file
        self.not_tested_dict = {}
        self.model_fields_dict = {}
        self.serializer_fields_dict = {}
        self._bad_data = []
        self.get_serializers_test_dict()

    def is_serializer_clean(self):
        return self.model_fields_dict == self.serializer_fields_dict

    @property
    def bad_data(self):
        return json.dumps(self._bad_data, indent=4)

    def get_serializers_test_dict(self):
        # Loop through serializers, create related models,
        # test that each equivalent field in the related models and serializers are eaqual
        for serializer_name, serializer in inspect.getmembers(self.serializer_file):
            if not getattr(serializer, "Meta", None):
                # If no meta data, do not test serializer
                self.not_tested_dict["serializers"] = self.not_tested_dict.get(
                    "serializers", []
                ) + [serializer_name]

            else:
                model_instance = get_fake_model_instance(model=serializer.Meta.model)
                serializers_instance = serializer(model_instance)

            # Get related model fields from serializer and test equivalence
            for serializer_field_name in serializers_instance.data:
                # Skip method fields - these should have custom tests
                if isinstance(
                    serializers_instance.fields[serializer_field_name],
                    serializers.SerializerMethodField,
                ):
                    self.not_tested_dict["fields"] = self.not_tested_dict.get(
                        "fields", {}
                    )
                    self.not_tested_dict["fields"][
                        serializer_name
                    ] = self.not_tested_dict.get(serializer_name, []) + [
                        serializer_field_name
                    ]
                    continue

                # Skip nested serializer fields - they will be tested independently in this loop
                elif isinstance(
                    serializers_instance.fields[serializer_field_name],
                    serializers.BaseSerializer,
                ):
                    continue

                model_field_name = serializer_field_name
                model_field_value = getattr(
                    model_instance, serializer_field_name, "field_does_not_exist"
                )
                serializer_field_value = serializers_instance.data[
                    serializer_field_name
                ]

                if model_field_value == "field_does_not_exist":
                    model_field_name = serializers_instance.fields[
                        serializer_field_name
                    ].source
                    model_field_value = model_instance

                    # Handle nested fields
                    if "." in model_field_name:
                        for related_field in model_field_name.split("."):
                            model_field_value = getattr(
                                model_field_value, related_field
                            )
                        model_field_name = related_field
                    else:
                        model_field_value = getattr(model_instance, model_field_name)

                # Serializer uses the __str__ definition for Model Fields
                if isinstance(model_field_value, Model):
                    model_field_value = str(model_field_value)

                # Handle serializers displaying ints a float
                if isinstance(model_field_value, (int, float)):
                    serializer_field_value = float(serializer_field_value)
                    model_field_value = float(model_field_value)

                # Python uses +00:00 instead of 'Z' for iso format
                if isinstance(model_field_value, datetime):
                    serializer_field_value = serializer_field_value[:-1]
                    model_field_value = model_field_value.isoformat()[:-6]

                # Handle querysets displayed as lists
                if isinstance(serializer_field_value, list) and not isinstance(
                    model_field_value, list
                ):
                    try:
                        model_field_value = list(model_field_value.all())
                    except Exception:
                        model_field_value = list(model_field_value)

                self.serializer_fields_dict[
                    serializer_name
                ] = self.serializer_fields_dict.get(serializer_name, []) + [
                    {
                        "serializer_field_name": serializer_field_name,
                        "field_value": serializer_field_value,
                    }
                ]
                self.model_fields_dict[serializer_name] = self.model_fields_dict.get(
                    serializer_name, []
                ) + [
                    {
                        "serializer_field_name": serializer_field_name,
                        "field_value": model_field_value,
                    }
                ]

                if serializer_field_value != model_field_value:
                    self._bad_data += [
                        {
                            "serializer_name": serializer_name,
                            "model_field_name": model_field_name,
                            "model_field_value": model_field_value,
                            "serializer_field_name": serializer_field_name,
                            "serializer_field_value": serializer_field_value,
                        }
                    ]
