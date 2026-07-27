{{/*
Expand the name of the chart.
*/}}
{{- define "msf.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" }}
{{- end }}

{{/*
Create a default fully qualified app name.
*/}}
{{- define "msf.fullname" -}}
{{- if .Values.fullnameOverride }}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- $name := default .Chart.Name .Values.nameOverride }}
{{- if contains $name .Release.Name }}
{{- .Release.Name | trunc 63 | trimSuffix "-" }}
{{- else }}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" }}
{{- end }}
{{- end }}
{{- end }}

{{/*
Common labels
*/}}
{{- define "msf.labels" -}}
helm.sh/chart: {{ include "msf.chart" . }}
{{ include "msf.selectorLabels" . }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}

{{- define "msf.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" }}
{{- end }}

{{- define "msf.selectorLabels" -}}
app.kubernetes.io/name: {{ include "msf.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
{{- end }}

{{/*
Service DNS name for an app key (identity, user, …).
*/}}
{{- define "msf.serviceName" -}}
{{- printf "%s-%s" (include "msf.fullname" .root) .name }}
{{- end }}

{{/*
GHCR image for an app key.
*/}}
{{- define "msf.image" -}}
{{- printf "%s/%s/msf-%s:%s" .Values.image.registry .Values.image.repositoryOwner .name .Values.image.tag }}
{{- end }}
